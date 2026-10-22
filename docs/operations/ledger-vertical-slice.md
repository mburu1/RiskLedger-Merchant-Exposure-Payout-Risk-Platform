# Ledger vertical slice

RiskLedger's ledger endpoints are synthetic accounting operations only; they do not initiate real payments.

## Database setup

Apply the PostgreSQL schema to the configured database:

```powershell
psql "$env:DATABASE_URL" -v ON_ERROR_STOP=1 -f backend/schema/ledger.sql
```

Alternatively configure `Database__ConnectionString` for ASP.NET Core. Never use the development fallback password outside local development.

## Endpoints

- `POST /api/v1/ledger/accounts` — create an account.
- `GET /api/v1/ledger/accounts` — list accounts.
- `POST /api/v1/ledger/entries` — post a balanced journal with an idempotency key.
- `GET /api/v1/ledger/entries` — immutable journal history.
- `POST /api/v1/ledger/entries/{id}/reversals` — post a compensating journal; never edits the original.
- `POST /api/v1/ledger/demo-posting` — synthetic example; KES seed accounts are included in the SQL schema.

## Invariants

- Exact decimal money with supported currency minor-unit precision.
- Each journal line has exactly one positive debit or credit.
- Journal debits equal credits; all lines in a request use one currency.
- Account codes and idempotency keys are unique.
- Journal account currency must match line currency.
- Idempotency-key reuse with a different payload is rejected.
- An original entry can be reversed at most once; PostgreSQL uniqueness enforces concurrency safety.
- Posted entries are immutable. Reversal means a new, linked journal.
