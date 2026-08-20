-- Per-query logging for /Search, plus lead/name capture and abuse blocking (known bad actors + NJ rDNS deny).
-- Run this against the PostgresqlProd database before deploying.

ALTER TABLE public."SearchLeads"
    ADD COLUMN IF NOT EXISTS "Name" text NOT NULL DEFAULT '',
    ADD COLUMN IF NOT EXISTS "ReverseDns" text NOT NULL DEFAULT '',
    ADD COLUMN IF NOT EXISTS "Blocked" boolean NOT NULL DEFAULT false,
    ADD COLUMN IF NOT EXISTS "BlockReason" text NOT NULL DEFAULT '';

CREATE TABLE IF NOT EXISTS public."SearchQueries"
(
    "SearchQueryId" uuid NOT NULL DEFAULT uuid_generate_v4(),
    "SearchLeadId" uuid NULL,
    "SessionId" text NOT NULL DEFAULT '',
    "Query" text NOT NULL DEFAULT '',
    "Email" text NOT NULL DEFAULT '',
    "ContactPhoneNumber" text NOT NULL DEFAULT '',
    "IpAddress" text NOT NULL DEFAULT '',
    "UserAgent" text NOT NULL DEFAULT '',
    "DateSearched" timestamp without time zone NOT NULL DEFAULT now(),
    CONSTRAINT "SearchQueries_pkey" PRIMARY KEY ("SearchQueryId"),
    CONSTRAINT "SearchQueries_SearchLeadId_fkey" FOREIGN KEY ("SearchLeadId")
        REFERENCES public."SearchLeads" ("SearchLeadId") ON DELETE SET NULL
);

CREATE INDEX IF NOT EXISTS "SearchQueries_Email_idx" ON public."SearchQueries" (LOWER("Email"));
CREATE INDEX IF NOT EXISTS "SearchQueries_ContactPhoneNumber_idx" ON public."SearchQueries" ("ContactPhoneNumber");
CREATE INDEX IF NOT EXISTS "SearchQueries_Query_idx" ON public."SearchQueries" ("Query");
CREATE INDEX IF NOT EXISTS "SearchQueries_IpAddress_idx" ON public."SearchQueries" ("IpAddress");
CREATE INDEX IF NOT EXISTS "SearchQueries_DateSearched_idx" ON public."SearchQueries" ("DateSearched" DESC);

-- Who queried a given number, and from where?
--
-- SELECT "Query", "Email", "ContactPhoneNumber", "IpAddress", "UserAgent", "DateSearched"
-- FROM public."SearchQueries"
-- WHERE "Query" = '2065551234'
-- ORDER BY "DateSearched" DESC;
