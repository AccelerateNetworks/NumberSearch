-- Address-level service availability, in support of https://acceleratenetworks.com/Internet.
-- Replaces the FCC census block lookup, which showed providers for every address in a block rather than the addresses we can actually sell.
-- Run this against the PostgresqlProd database before deploying, then load the building lists with Ziply/import_ziply_building_list.py.

CREATE TABLE IF NOT EXISTS public."ServiceAddresses"
(
    "ServiceAddressId" bigserial NOT NULL,
    "Provider" text NOT NULL DEFAULT '',
    -- WFI (wholesale fiber internet) or EIA (ethernet internet access)
    "Product" text NOT NULL DEFAULT '',
    -- Sellable: both of the provider's serviceability flags agree, quote the listed price.
    -- Confirm: only one flag is set, ask the customer to contact us before quoting.
    -- Quote: on-net for a custom priced product like EIA.
    "Status" text NOT NULL DEFAULT '',
    "HouseNumber" text NOT NULL DEFAULT '',
    -- Street name lowercased with everything but letters and digits removed, ex. "Pine Cliff" -> "pinecliff"
    "StreetKey" text NOT NULL DEFAULT '',
    "StreetAddress" text NOT NULL DEFAULT '',
    "City" text NOT NULL DEFAULT '',
    "State" text NOT NULL DEFAULT '',
    "Postal" text NOT NULL DEFAULT '',
    "Latitude" double precision NOT NULL DEFAULT 0,
    "Longitude" double precision NOT NULL DEFAULT 0,
    "MaxSpeed" text NOT NULL DEFAULT '',
    "BuildingKey" text NOT NULL DEFAULT '',
    "SourceFile" text NOT NULL DEFAULT '',
    "DateIngested" timestamp without time zone NOT NULL DEFAULT now(),
    CONSTRAINT "ServiceAddresses_pkey" PRIMARY KEY ("ServiceAddressId")
);

CREATE INDEX IF NOT EXISTS "ServiceAddresses_Address_idx" ON public."ServiceAddresses" ("Postal", "HouseNumber");
CREATE INDEX IF NOT EXISTS "ServiceAddresses_Street_idx" ON public."ServiceAddresses" ("Postal", "StreetKey");
-- One row per product and building, so Cart/Add re-qualifies the same building the Internet page offered. A list that breaks this fails the import, which leaves the existing rows in place.
CREATE UNIQUE INDEX IF NOT EXISTS "ServiceAddresses_Product_BuildingKey_key" ON public."ServiceAddresses" ("Product", "BuildingKey") WHERE "BuildingKey" <> '';
DROP INDEX IF EXISTS public."ServiceAddresses_BuildingKey_idx";
CREATE INDEX IF NOT EXISTS "ServiceAddresses_Location_idx" ON public."ServiceAddresses" ("Latitude", "Longitude");

ALTER TABLE public."ServiceAddresses" OWNER TO "numberSearch";

-- The fiber internet tiers sold at WFI Sellable addresses. $15 off when bundled with any phone service, on a 2, 3 or 5 year term.
INSERT INTO public."Services" ("ServiceId", "Name", "Price", "Description")
VALUES ('cbcf5128-5164-40de-8dff-e71d0f152cab', 'Fiber Internet 300 Mbps', 75, '300/300 Mbps fiber internet on a 2, 3 or 5 year term. $15 off when bundled with phone service.'),
       ('708c3885-6dab-4a60-9e42-05cf13530076', 'Fiber Internet 1 Gbps', 115, '1/1 Gbps fiber internet on a 2, 3 or 5 year term. $15 off when bundled with phone service.')
ON CONFLICT ("ServiceId") DO NOTHING;
