# M0 Acceptance and Verification Checklist

This document defines the acceptance gates for the first RiskLedger implementation milestone. A milestone is complete only when its acceptance criteria pass on a clean checkout and its commit is recorded.

## M0.1 — Architecture and engineering conventions

### Acceptance criteria
- [ ] Architecture decision records explain the modular-monolith boundary, PostgreSQL as the financial source of truth, double-entry posting, money/currency precision, and idempotency.
- [ ] A domain glossary defines account, journal entry, journal line, posting, reversal, idempotency key, and currency minor-unit precision.
- [ ] Financial invariants state that amounts use C# `decimal`, entries balance per currency, posted entries are immutable, and corrections use compensating entries.
- [ ] Dependency rules keep Domain independent of Infrastructure and API frameworks; Infrastructure implements Application ports.
- [ ] SDK and package versions are reproducible; repository formatting conventions are checked in.
- [ ] CI runs on pushes and pull requests to `main`.

### Verification
From the repository root (PowerShell):

```powershell
dotnet --info
dotnet format .\backend\RiskLedger.sln --verify-no-changes
```

Inspect `.github/workflows/ci.yml` and the ADR/glossary documents. Do not mark formatting as passed unless the command exits with code 0.

## M0.2 — Runnable solution skeleton

### Acceptance criteria
- [ ] The .NET solution restores and builds.
- [ ] The Angular shell installs reproducibly and builds.
- [ ] PostgreSQL Compose configuration validates and starts.
- [ ] Scalar/OpenAPI is available in Development.
- [ ] `/health/live` reports process liveness independently of database availability.
- [ ] `/health/ready` checks PostgreSQL connectivity.
- [ ] Invalid database configuration fails validation at startup; local defaults are clearly simulation-only.
- [ ] CI verifies backend and frontend build steps.

### Verification
From the repository root (PowerShell):

```powershell
dotnet restore .\backend\RiskLedger.sln
dotnet build .\backend\RiskLedger.sln --configuration Release --no-restore
dotnet test .\backend\RiskLedger.sln --configuration Release --no-build
docker compose -f .\infrastructure\compose\compose.yaml config
docker compose -f .\infrastructure\compose\compose.yaml up -d
docker compose -f .\infrastructure\compose\compose.yaml ps
```

In another terminal, from the frontend directory:

```powershell
Set-Location .\frontend\riskledger-web
npm ci
npm run lint
npm test
npm run build
```

Confirm the API health endpoints and Scalar page using the actual URL/port configured for the local launch profile. Stop local services with `docker compose -f .\infrastructure\compose\compose.yaml down` when finished.

## M0.3 — First ledger vertical slice

### Acceptance criteria
- [ ] Money validates supported currency and amount precision and uses `decimal`.
- [ ] Accounts are persisted with unique account codes.
- [ ] Journal posting rejects malformed lines and entries that do not balance per currency.
- [ ] Posted journal entries and their lines are immutable through normal application commands.
- [ ] A unique idempotency key prevents duplicate financial effects across retries.
- [ ] A reversal creates a compensating entry linked to the original and does not delete or mutate it.
- [ ] PostgreSQL schema/migrations can create the required tables from an empty database.
- [ ] Tests cover valid posting, unbalanced posting, duplicate idempotency key, reversal, and invalid reversal.
- [ ] Ledger history can be retrieved through the API.

### Verification
Run the backend restore/build/test commands above, then start PostgreSQL and apply the committed EF Core migrations using the repository's configured startup project and migrations assembly. Confirm that accounts and entries survive an API restart. Exercise the account, posting, history, and reversal endpoints; repeat a posting with the same idempotency key and verify that no second financial entry is created.

## Commit discipline

Use a separate commit for each milestone, with messages in this form:

- `chore(m0.1): establish architecture and engineering conventions`
- `build(m0.2): add runnable solution skeleton`
- `feat(m0.3): implement persistent idempotent ledger posting`

A commit existing on GitHub is not proof that the acceptance criteria passed. Record actual command results and CI status before calling a milestone verified. Database-backed integration tests must run against PostgreSQL; a successful compile alone is insufficient.

## Current verification note

The repository contains implementation commits for the PostgreSQL ledger and idempotency/reversal behavior. The CI status query returned no reported status checks for the latest ledger test commit at the time this checklist was prepared. Run the commands above and inspect the GitHub Actions run before describing the milestones as fully verified.
