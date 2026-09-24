#!/usr/bin/env python3
"""Load a Ziply Fiber building list (.xlsx) into public."ServiceAddresses".

Usage: import_ziply_building_list.py WFI|EIA "<building list>.xlsx" [psql connection args...]
  ex. import_ziply_building_list.py WFI "/WFI 24635_20260920050445.xlsx" -d numberSearch

The lists are 500MB+ of inline string XML once unzipped, so rows are streamed and cleared as they
are read rather than loaded with openpyxl. Peak memory is about 45MB for the 117k row WFI list, most of it the
building keys held to catch duplicates. Only the rows we can sell
or quote are kept, and the previous rows for the same product are replaced in a single transaction.
Uses only the standard library and psql.
"""
import csv
import os
import re
import subprocess
import sys
import tempfile
import zipfile
from xml.etree.ElementTree import iterparse

NS = '{http://schemas.openxmlformats.org/spreadsheetml/2006/main}'
COLUMNS = ['Provider', 'Product', 'Status', 'HouseNumber', 'StreetKey', 'StreetAddress', 'City', 'State',
           'Postal', 'Latitude', 'Longitude', 'MaxSpeed', 'BuildingKey', 'SourceFile']


def street_key(name):
    # Must match ServiceAddress.ToStreetKey in NumberSearch.DataAccess.
    return re.sub(r'[^a-z0-9]', '', name.lower())


def column_index(ref):
    n = 0
    for ch in re.match(r'[A-Z]+', ref).group():
        n = n * 26 + ord(ch) - 64
    return n - 1


def shared_strings(z):
    """The workbook's shared string table. Ziply's exports use inline strings, but a list re-saved in Excel uses this."""
    if 'xl/sharedStrings.xml' not in z.namelist():
        return []
    strings = []
    with z.open('xl/sharedStrings.xml') as f:
        for _, el in iterparse(f, events=('end',)):
            if el.tag == NS + 'si':
                strings.append(''.join(t.text or '' for t in el.iter(NS + 't')))
                el.clear()
    return strings


def cell_text(c, strings):
    if c.get('t') == 's':
        v = c.find(NS + 'v')
        return strings[int(v.text)] if v is not None and v.text else ''
    text = ''.join(t.text or '' for t in c.iter(NS + 't'))
    if not text:
        v = c.find(NS + 'v')
        text = v.text or '' if v is not None else ''
    return text


class NoHeader(Exception):
    pass


def rows(path):
    """Yield each row after the header as a dict keyed by column name."""
    header = None
    with zipfile.ZipFile(path) as z:
        strings = shared_strings(z)
        with z.open('xl/worksheets/sheet1.xml') as sheet:
            for _, el in iterparse(sheet, events=('end',)):
                if el.tag != NS + 'row':
                    continue
                values = {}
                for position, c in enumerate(el.findall(NS + 'c')):
                    # The cell reference is optional, in which case cells are in column order.
                    ref = c.get('r')
                    values[column_index(ref) if ref else position] = cell_text(c, strings).strip()
                el.clear()
                if header is None:
                    if 'Building Name' in values.values() and 'Street Address' in values.values():
                        header = values
                    continue
                if values:
                    yield {name: values.get(i, '') for i, name in header.items()}
    if header is None:
        raise NoHeader(f'No header row with "Building Name" and "Street Address" found in {path}. Is this a Ziply building list?')


def list_rows(path):
    """rows(), but reading the header before any row is written so a missing header fails before the database is touched."""
    it = rows(path)
    first = next(it, None)
    if first is None:
        return iter(())
    return _chain(first, it)


def _chain(first, it):
    yield first
    yield from it


def status(product, row):
    if product == 'WFI':
        serviceable = row.get('BFI/WFI serviceable') == 'Y'
        offered = row.get('PRODUCT') == 'BFI/WFI'
        if serviceable and offered:
            return 'Sellable'
        if serviceable or offered:
            return 'Confirm'
        return None
    return 'Quote' if row.get('EIA On-Net') == 'Y' else None


def main():
    if len(sys.argv) < 3 or sys.argv[1] not in ('WFI', 'EIA'):
        sys.exit(__doc__)
    product, path, psql_args = sys.argv[1], sys.argv[2], sys.argv[3:]
    source = os.path.basename(path)
    kept = skipped = blank_sellable = 0
    # Cart/Add re-qualifies a building by its key, so each key must be one row. Checked here so a bad list fails before the database is touched.
    seen_keys = {}
    duplicate_keys = []

    with tempfile.NamedTemporaryFile('w', newline='', suffix='.csv', delete=False) as out:
        # Quote everything so empty values load as empty strings rather than NULL.
        writer = csv.writer(out, quoting=csv.QUOTE_ALL)
        try:
            listed = list_rows(path)
        except NoHeader as e:
            os.unlink(out.name)
            sys.exit(f'{e} Leaving the existing rows in place.')
        for row in listed:
            s = status(product, row)
            if s is None:
                skipped += 1
                continue
            try:
                lat, lon = float(row.get('Latitude') or 0), float(row.get('Longitude') or 0)
            except ValueError:
                lat = lon = 0.0
            key = row.get('C2F Building Key', '')
            if key:
                if key in seen_keys and len(duplicate_keys) < 5:
                    duplicate_keys.append(f"{key} ({seen_keys[key]} and {row.get('Street Address', '').strip()})")
                seen_keys.setdefault(key, row.get('Street Address', '').strip())
            elif s == 'Sellable':
                blank_sellable += 1
            writer.writerow([row.get('Provider') or 'Ziply Fiber', product, s, row.get('Primary Number', ''),
                             street_key(row.get('Street Name', '')), row.get('Street Address', ''),
                             row.get('City', ''), row.get('State', ''), row.get('Postal', '')[:5], lat, lon,
                             row.get('BFI Max Serviceable Speed', ''), row.get('C2F Building Key', ''), source])
            kept += 1

    if duplicate_keys:
        os.unlink(out.name)
        sys.exit(f'{path} lists the same C2F Building Key on more than one {product} row, ex. {"; ".join(duplicate_keys)}. '
                 'Leaving the existing rows in place.')

    if blank_sellable:
        print(f'Warning: {blank_sellable} Sellable {product} rows have no C2F Building Key. The Internet page will ask those customers to contact us.')

    if kept == 0:
        os.unlink(out.name)
        sys.exit(f'Found the header in {path} but no {product} rows we can sell or quote, leaving the existing rows in place.')

    columns = ', '.join(f'"{c}"' for c in COLUMNS)
    script = (
        'BEGIN;\n'
        f"DELETE FROM public.\"ServiceAddresses\" WHERE \"Product\" = '{product}';\n"
        f"\\copy public.\"ServiceAddresses\" ({columns}) FROM '{out.name}' WITH (FORMAT csv)\n"
        'COMMIT;\n'
        'ANALYZE public."ServiceAddresses";\n'
    )
    try:
        subprocess.run(['psql', '-v', 'ON_ERROR_STOP=1', *psql_args], input=script, text=True, check=True)
    except subprocess.CalledProcessError:
        sys.exit(f'psql failed loading {source}, see the error above. The import runs in one transaction, so the existing rows are still in place.')
    except FileNotFoundError:
        sys.exit(f'psql is not installed or not on the PATH, so {source} was not loaded. The existing rows are still in place.')
    finally:
        os.unlink(out.name)
    print(f'Loaded {kept} {product} addresses from {source}, skipped {skipped} not serviceable.')


if __name__ == '__main__':
    main()
