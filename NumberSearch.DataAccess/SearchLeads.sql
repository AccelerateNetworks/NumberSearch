-- Search page lead capture, in support of gating https://acceleratenetworks.com/Search behind an introduction.
-- Run this against the PostgresqlProd database before deploying.

CREATE TABLE IF NOT EXISTS public."SearchLeads"
(
    "SearchLeadId" uuid NOT NULL DEFAULT uuid_generate_v4(),
    "SessionId" text NOT NULL DEFAULT '',
    "ContactPhoneNumber" text NOT NULL DEFAULT '',
    "Email" text NOT NULL DEFAULT '',
    "EmailDomain" text NOT NULL DEFAULT '',
    "MxRecordExists" boolean NOT NULL DEFAULT false,
    "ContactPhoneNumberPortable" boolean NOT NULL DEFAULT false,
    "Query" text NOT NULL DEFAULT '',
    "IpAddress" text NOT NULL DEFAULT '',
    "UserAgent" text NOT NULL DEFAULT '',
    "Referrer" text NOT NULL DEFAULT '',
    "DateSubmitted" timestamp without time zone NOT NULL DEFAULT now(),
    CONSTRAINT "SearchLeads_pkey" PRIMARY KEY ("SearchLeadId")
);

CREATE INDEX IF NOT EXISTS "SearchLeads_Email_idx" ON public."SearchLeads" (LOWER("Email"));
CREATE INDEX IF NOT EXISTS "SearchLeads_SessionId_idx" ON public."SearchLeads" ("SessionId");
CREATE INDEX IF NOT EXISTS "SearchLeads_DateSubmitted_idx" ON public."SearchLeads" ("DateSubmitted" DESC);

CREATE TABLE IF NOT EXISTS public."SearchLeadCartItems"
(
    "SearchLeadCartItemId" uuid NOT NULL DEFAULT uuid_generate_v4(),
    "SearchLeadId" uuid NULL,
    "SessionId" text NOT NULL DEFAULT '',
    "ProductType" text NOT NULL DEFAULT '',
    "ProductIdentifier" text NOT NULL DEFAULT '',
    "Quantity" integer NOT NULL DEFAULT 1,
    "DateAddedToCart" timestamp without time zone NOT NULL DEFAULT now(),
    CONSTRAINT "SearchLeadCartItems_pkey" PRIMARY KEY ("SearchLeadCartItemId"),
    CONSTRAINT "SearchLeadCartItems_SearchLeadId_fkey" FOREIGN KEY ("SearchLeadId")
        REFERENCES public."SearchLeads" ("SearchLeadId") ON DELETE SET NULL
);

CREATE INDEX IF NOT EXISTS "SearchLeadCartItems_SearchLeadId_idx" ON public."SearchLeadCartItems" ("SearchLeadId");
CREATE INDEX IF NOT EXISTS "SearchLeadCartItems_SessionId_idx" ON public."SearchLeadCartItems" ("SessionId");
CREATE INDEX IF NOT EXISTS "SearchLeadCartItems_DateAddedToCart_idx" ON public."SearchLeadCartItems" ("DateAddedToCart" DESC);

-- What is each lead shopping for?
--
-- SELECT l."DateSubmitted", l."Email", l."ContactPhoneNumber", l."Query",
--        i."DateAddedToCart", i."ProductType", i."ProductIdentifier", i."Quantity"
-- FROM public."SearchLeads" l
-- LEFT JOIN public."SearchLeadCartItems" i ON i."SearchLeadId" = l."SearchLeadId"
-- ORDER BY l."DateSubmitted" DESC, i."DateAddedToCart";
