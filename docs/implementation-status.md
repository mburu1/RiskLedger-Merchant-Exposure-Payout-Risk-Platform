# Implementation status

Last reviewed: 2026-10-22

This document distinguishes committed implementation evidence from the intended product roadmap. A commit existing is not proof that its code has passed a clean local build or CI run; verify the latest workflow before claiming a release-ready state.

## Completed foundation / in-progress vertical slice

- [x] Repository product framing and financial invariants are documented.
- [x] Initial engineering conventions and CI workflow are present.
- [x] .NET 10 solution and API foundation are present.
- [x] Angular application shell, routing, and typed readiness view are present.
- [x] PostgreSQL Compose configuration and EF Core/Npgsql integration are present.
- [x] Liveness/readiness endpoints, development-only OpenAPI/Scalar, structured JSON logging, and Problem Details handling are present.
- [x] Money/currency rules and double-entry journal validation are represented in the domain.
- [x] Ledger account, journal posting, history, and compensating reversal API paths are present.
- [x] A PostgreSQL ledger adapter has been wired into the API in the current source.

## Must verify before treating the ledger slice as complete

- [ ] Run restore, Release build, and all tests from a clean checkout.
- [ ] Confirm EF Core migrations exist and apply to an empty PostgreSQL database.
- [ ] Confirm journal entry, journal lines, and idempotency key are committed atomically.
- [ ] Confirm database uniqueness protects idempotency under concurrent requests and a reused key with a different payload is rejected.
- [ ] Confirm posted journal rows cannot be modified/deleted through ordinary application paths.
- [ ] Confirm reversal links, original-entry preservation, and transaction rollback behavior.
- [ ] Run concurrent duplicate-posting and competing-posting tests against PostgreSQL/Testcontainers.
- [ ] Verify current GitHub Actions workflow results for the latest main commit.

## Remaining roadmap

### Phase 2 — Core domain persistence
- Organizer, verification case, trip, booking, supplier obligation, and dispute models.
- EF Core mappings, migrations, indexes, foreign keys, audit metadata, and concurrency policy.
- Deterministic synthetic seed data and ERD.

### Phase 4 — Exposure calculation
- Obligation eligibility/lifecycle rules and settlement effects.
- Versioned deterministic calculation snapshots and organizer/trip breakdowns.
- Currency-safe totals, partial settlements, missing data, future dates, and duplicate-input tests.

### Phase 5 — Risk policy
- Immutable policy versions, deterministic assessments, reason codes and canonical input snapshots.
- Review queue, authorized override flow, and audit records.

### Phase 6 — Reserves
- Reserve policy versions, allocation/release lifecycle, ledger-backed movements and idempotency.
- Reassessment triggers and race-condition coverage.

### Phase 7 — Payouts
- Eligibility checks based on authoritative settled funds, holds, reserves, verification, risk state and lifecycle.
- Atomic funds commitment, idempotent payout requests, simulated provider success/decline/timeout/unknown outcomes.
- Safe retry and reconciliation for ambiguous provider outcomes.

### Phase 8 — Messaging and workers
- Transactional outbox, RabbitMQ, idempotent/versioned consumers, bounded retries, dead-letter handling, worker recovery and metrics.

### Phase 9 — Operations console
- API-backed dashboard, organizer timeline, risk review, exposure, reserves, payouts, disputes, ledger and audit screens.
- Server-side pagination/filtering, accessible loading/error/empty states and critical workflow tests.

### Phase 10 — Security and reconciliation
- Authentication and granular authorization for sensitive actions.
- Simulated provider statement import, discrepancy lifecycle, rate limiting, secure configuration, redacted logs, metrics/traces/alerts, and recovery runbooks.

### Phase 11 — Delivery hardening
- Reliable lockfile-based frontend CI, lint/test/build, backend integration services, migration validation and dependency/security checks.
- Container image build, clean Compose startup, graceful shutdown, load/concurrency measurements, backup/restore exercise and verified documentation.
- Kubernetes remains deferred until local containerized behavior is stable.

### Phase 12 — Optional advisory AI
- Provider-neutral case-summary adapter with source references, minimized data, human review and evaluation tests.
- AI must never post ledger entries or authorize payouts.

## Recommended next implementation order

1. Verify and harden the PostgreSQL ledger vertical slice and its migrations/concurrency tests.
2. Implement Phase 2 domain persistence and deterministic seed data.
3. Implement exposure calculations (Phase 4).
4. Implement versioned risk assessments (Phase 5).
5. Implement reserves before payouts (Phase 6, then Phase 7).
6. Add outbox/workers, UI workflows, reconciliation/security, and delivery hardening.
7. Consider advisory AI only after operational and authorization boundaries are established.

## Release boundary

RiskLedger remains an educational simulation. It is not a production financial platform, does not move real money, and must use synthetic data and simulated providers. Do not describe the project as complete until the roadmap acceptance criteria and clean-checkout verification are satisfied.
