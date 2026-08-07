-- NG911/E911 affirmative acknowledgment for orders containing voice products (numbers, seats, hardware).
-- Run this against the PostgresqlProd database before deploying.

ALTER TABLE public."Orders"
    ADD COLUMN IF NOT EXISTS "VoiceProductAcknowledged" boolean NOT NULL DEFAULT false,
    ADD COLUMN IF NOT EXISTS "VoiceProductAcknowledgedUtc" timestamp without time zone NULL;
