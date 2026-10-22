# Implementation status

Last reviewed: 2026-10-22

This document distinguishes committed implementation evidence from the intended product roadmap. The new domain components are pure policy/calculation helpers; they are not yet wired to authoritative database queries, transactional use cases, or API endpoints. A commit existing is not proof that its code has passed a clean build or CI run.

## Committed foundation

- [x] Product framing, engineering conventions, ADRs and financial invariants are documented.
- [x] .NET 10 solution/API and Angular application shell are present.
- [x] PostgreSQL Compose configuration and EF Core/Npgsql integration are present.
- [x] Liveness/readiness endpoints, development-only OpenAPI/Scalar, JSON logging and Problem Details handling are present.
- [x] Money/currency rules and double-entry journal validation are represented in the domain.
- [x] Ledger account, journal posting, history and compensating reversal API paths are present.
- [x] PostgreSQL ledger adapter is wired into the API.
- [x] Pure deterministic exposure calculator with eligible-source ID tracking and duplicate/mixed-currency guards is present.
- [x] Deterministic risk-policy evaluator with version and reason codes is present.
- [x] Pure reserve requirement/releasable-amount calculation is present.
- [x] Fail-closed payout eligibility helper and payout lifecycle transition rules are present.
- [x] Pure simulated-statement reconciliation matcher detects missing, duplicate and mismatched records.
- [x] backend/schema/platform.sql defines initial relational tables for organizers, verification, trips, bookings, obligations, disputes, exposure snapshots, policy versions, assessments, reserves, payouts, audit, outbox/inbox and reconciliation.

## Verification status — not yet release-ready

- [ ] Run restore, Release build, and all tests from a clean checkout.
- [ ] Confirm latest GitHub Actions runs pass for the latest main commit.
- [ ] Apply both schema scripts to an empty PostgreSQL database and verify constraints/indexes.
- [ ] Convert the platform schema into tracked EF Core migrations and map all platform entities.
- [ ] Wire exposure calculations to persisted eligible records and persist immutable input snapshots.
- [ ] Persist risk policy versions/assessments and add review/override APIs with authorization/audit.
- [ ] Implement ledger-backed reserve movements, transactional holds and idempotent release commands.
- [ ] Implement atomic payout reservation, idempotency conflict handling, and simulated provider adapter.
- [ ] Add PostgreSQL/Testcontainers integration and concurrency tests for ledger, reserves, payouts and migrations.
- [ ] Implement a transactional outbox publisher, RabbitMQ consumer, inbox deduplication, retry/dead-letter policy and worker recovery.
- [ ] Wire reconciliation matcher to statement import, persisted findings, resolution workflow and API.
- [ ] Build API-backed Angular operations workflows; the existing shell is not the completed operations console.
- [ ] Add authentication, granular authorization, rate limiting, secure configuration, redaction and observability.
- [ ] Add clean Compose/CI verification, dependency scanning, backup/restore and operational runbooks.

## Phase map

| Phase | Scope | Current state |
|---|---|---|
| P0 | Product framing, ADRs, conventions, CI foundation | Initial foundation committed; clean CI verification still required |
| P1 | Runnable backend/frontend/PostgreSQL skeleton | Initial skeleton present; clean startup/build still needs verification |
| P2 | Core domain persistence and migrations | SQL schema draft committed; EF mappings/migrations and APIs remain |
| P3 | Double-entry ledger | Initial API/store/domain slice present; database concurrency and migration checks remain |
| P4 | Exposure | Deterministic pure calculator committed; persisted-source integration remains |
| P5 | Risk underwriting | Deterministic pure policy evaluator committed; persistence, review queue and authorization remain |
| P6 | Reserves | Calculation helper committed; allocation/release workflows and ledger-backed movements remain |
| P7 | Payouts | Eligibility and transition helpers committed; atomic reservation/provider/recovery integration remains |
| P8 | Messaging/workers | Outbox/inbox schema present; publisher, broker, consumer and worker remain |
| P9 | Operations console | Angular shell present; product workflows and frontend tests remain |
| P10 | Reconciliation/security/reliability | Pure reconciliation matcher and schema committed; import/resolution/auth/observability remain |

## Next implementation order

1. Validate/fix build and tests against a clean checkout and inspect CI results.
2. Introduce EF Core migrations and persistence mappings for the new platform schema.
3. Implement persisted organizer/trip/booking/obligation/verification/dispute use cases and deterministic synthetic seed data.
4. Wire exposure and risk use cases to persisted source-of-truth data.
5. Add transactional reserve and payout workflows with PostgreSQL concurrency tests.
6. Implement outbox/RabbitMQ workers and provider reconciliation.
7. Finish authorization, operations console, observability, security and delivery hardening.

## Release boundary

RiskLedger remains an educational simulation. It is not a production financial platform, does not move real money, and must use synthetic data and simulated providers. The pure helpers are not a substitute for transactional enforcement in the database/application layer. Do not describe the project as complete until all phase acceptance criteria and clean-checkout verification pass.
