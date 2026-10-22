-- RiskLedger ledger schema; apply once to the configured PostgreSQL database.
CREATE TABLE IF NOT EXISTS ledger_accounts (
    "Code" varchar(64) PRIMARY KEY,
    "Name" varchar(160) NOT NULL,
    "Currency" varchar(3) NOT NULL,
    "AccountType" varchar(16) NOT NULL CHECK ("AccountType" IN ('asset','liability','equity','revenue','expense')),
    "IsActive" boolean NOT NULL DEFAULT true,
    CONSTRAINT ck_ledger_accounts_currency CHECK (length("Currency") = 3)
);

CREATE TABLE IF NOT EXISTS journal_entries (
    "Id" uuid PRIMARY KEY,
    "IdempotencyKey" varchar(200) NOT NULL UNIQUE,
    "Description" varchar(500) NOT NULL,
    "PostedAt" timestamp with time zone NOT NULL,
    "ReversalOfEntryId" uuid NULL REFERENCES journal_entries("Id") ON DELETE RESTRICT,
    "PayloadHash" varchar(64) NOT NULL,
    CONSTRAINT uq_journal_entries_single_reversal UNIQUE ("ReversalOfEntryId")
);

CREATE INDEX IF NOT EXISTS ix_journal_entries_posted_at ON journal_entries ("PostedAt" DESC);

CREATE TABLE IF NOT EXISTS journal_lines (
    "Id" uuid PRIMARY KEY,
    "JournalEntryId" uuid NOT NULL REFERENCES journal_entries("Id") ON DELETE RESTRICT,
    "AccountCode" varchar(64) NOT NULL REFERENCES ledger_accounts("Code") ON DELETE RESTRICT,
    "Debit" numeric(20,3) NOT NULL DEFAULT 0 CHECK ("Debit" >= 0),
    "Credit" numeric(20,3) NOT NULL DEFAULT 0 CHECK ("Credit" >= 0),
    "Currency" varchar(3) NOT NULL,
    CONSTRAINT ck_journal_lines_one_side CHECK (("Debit" > 0 AND "Credit" = 0) OR ("Credit" > 0 AND "Debit" = 0))
);

CREATE INDEX IF NOT EXISTS ix_journal_lines_entry_account ON journal_lines ("JournalEntryId", "AccountCode");

-- Seed the two illustrative accounts. Safe to re-run; this is synthetic accounting only.
INSERT INTO ledger_accounts ("Code","Name","Currency","AccountType","IsActive")
VALUES ('1000-PROCESSOR-CLEARING','Processor clearing asset','KES','asset',true),
       ('2000-MERCHANT-FUNDS-PAYABLE','Merchant funds payable liability','KES','liability',true)
ON CONFLICT ("Code") DO NOTHING;
