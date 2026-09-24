-- Fiber internet contract terms and the phone bundle discount, in support of https://acceleratenetworks.com/Internet.
-- Run this against the PostgresqlProd database before deploying.

-- The contract term (2, 3 or 5 years) chosen for fiber internet on an order, 0 when the order has no fiber internet.
ALTER TABLE public."Orders"
    ADD COLUMN IF NOT EXISTS "InternetTermYears" integer NOT NULL DEFAULT 0;

-- The Partner coupon now also takes $15/mo off each fiber internet connection, see InternetBundle.cs.
UPDATE public."Coupons"
SET "Description" = '5G service and fiber internet at partner pricing.'
WHERE "CouponId" = '245e1c8a-0208-4016-acd1-a36f45315e88';
