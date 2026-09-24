-- Fiber internet contract terms and the phone bundle discount, in support of https://acceleratenetworks.com/Internet.
-- Run this against the PostgresqlProd database with psql before deploying. Safe to run again.

-- Stop at the first failed statement, however this file is run, so a failed CREATE never falls through to a later DROP.
\set ON_ERROR_STOP on

-- The contract term (2, 3 or 5 years) chosen for fiber internet on an order, 0 when the order has no fiber internet.
ALTER TABLE public."Orders"
    ADD COLUMN IF NOT EXISTS "InternetTermYears" integer NOT NULL DEFAULT 0,
    -- The listed service address the fiber was qualified at, and the provider's building key, which survives re-importing the building lists.
    ADD COLUMN IF NOT EXISTS "InternetServiceAddress" text NOT NULL DEFAULT '',
    ADD COLUMN IF NOT EXISTS "InternetBuildingKey" text NOT NULL DEFAULT '';

-- The Partner coupon now also takes $15/mo off each fiber internet connection, see InternetBundle.cs.
UPDATE public."Coupons"
SET "Description" = '5G service and fiber internet at partner pricing.'
WHERE "CouponId" = '245e1c8a-0208-4016-acd1-a36f45315e88';
