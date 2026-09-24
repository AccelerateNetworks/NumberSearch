#!/usr/bin/env python3
"""Load a Ziply Fiber building list (.xlsx) into public."ServiceAddresses".

Usage: import_ziply_building_list.py WFI|EIA "<building list>.xlsx" [psql connection args...]
  ex. import_ziply_building_list.py WFI "/WFI 24635_20260920050445.xlsx" -d numberSearch

The lists are 500MB+ of inline string XML once unzipped, so rows are streamed and cleared as they
are read rather than loaded with openpyxl. Peak memory stays around 20MB. Only the rows we can sell
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


def rows(path):
    """Yield each row after the header as a dict keyed by column name."""
    header = None
    with zipfile.ZipFile(path) as z, z.open('xl/worksheets/sheet1.xml') as sheet:
        for _, el in iterparse(sheet, events=('end',)):
            if el.tag != NS + 'row':
                continue
            values = {}
            for c in el.findall(NS + 'c'):
                text = ''.join(t.text or '' for t in c.iter(NS + 't'))
                if not text:
                    v = c.find(NS + 'v')
                    text = v.text or '' if v is not None else ''
                values[column_index(c.get('r'))] = text.strip()
            el.clear()
            if header is None:
                if values.get(0) == 'Building Name':
                    header = values
                continue
            if values:
                yield {name: values.get(i, '') for i, name in header.items()}


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
    kept = skipped = 0

    with tempfile.NamedTemporaryFile('w', newline='', suffix='.csv', delete=False) as out:
        # Quote everything so empty values load as empty strings rather than NULL.
        writer = csv.writer(out, quoting=csv.QUOTE_ALL)
        for row in rows(path):
            s = status(product, row)
            if s is None:
                skipped += 1
                continue
            try:
                lat, lon = float(row.get('Latitude') or 0), float(row.get('Longitude') or 0)
            except ValueError:
                lat = lon = 0.0
            writer.writerow([row.get('Provider') or 'Ziply Fiber', product, s, row.get('Primary Number', ''),
                             street_key(row.get('Street Name', '')), row.get('Street Address', ''),
                             row.get('City', ''), row.get('State', ''), row.get('Postal', '')[:5], lat, lon,
                             row.get('BFI Max Serviceable Speed', ''), row.get('C2F Building Key', ''), source])
            kept += 1

    if kept == 0:
        os.unlink(out.name)
        sys.exit(f'No {product} rows found in {path}, leaving the existing rows in place.')

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
    finally:
        os.unlink(out.name)
    print(f'Loaded {kept} {product} addresses from {source}, skipped {skipped} not serviceable.')


if __name__ == '__main__':
    main()
