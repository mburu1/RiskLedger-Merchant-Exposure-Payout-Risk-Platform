# ADR-0003: Financial invariants before schema and payout implementation

- Status: Accepted for the educational simulation
- Date: 2026-10-22
- Decision owners: RiskLedger project

## Context

RiskLedger determines whether merchant funds cover supplier obligations and whether a payout is permissible. A generic mutable balance or a risk score alone cannot support a reliable financial decision. Schema design must follow explicit accounting semantics.

## Decisions

1. Use decimal and a currency-aware Money value object. Support only currencies explicitly listed in the domain catalogue, reject excess input precision, and use midpoint-to-even only through an explicit rounding operation.
2. Use double-entry journals as the accounting source of truth. Every posted entry must balance independently for each currency.
3. Treat posted journal entries as immutable. Corrections use compensating entries linked to their originals.
4. Require idempotency keys for financial commands. In durable storage, enforce uniqueness and store the command result atomically with the financial movement.
5. Define pending, settled, allocated, restricted, payable, and unknown-outcome funds separately. Do not treat pending funds as payout-available by default.
6. Calculate exposure from eligible obligations and explicitly allocated qualifying coverage. Prevent the same funds from covering multiple obligations.
7. Persist policy version, reproducible input snapshot, outcome, reason codes, timestamp, and audit references for each risk assessment.
8. Gate payouts on verification, authoritative available funds, reserve/hold state, current risk restrictions, lifecycle, and idempotency. Commit the funds reservation atomically before provider submission.
9. Treat provider timeouts as ambiguous outcomes requiring reconciliation; a timeout is not evidence of decline or success.
10. Keep the modular monolith and PostgreSQL as the planned source of truth. Do not add broker or service boundaries before the transactional core is correct.

## Consequences

- The currency catalogue is explicit and must be extended intentionally.
- API validation and database constraints complement, but never replace, domain invariants.
- PostgreSQL migrations must include unique idempotency constraints, immutable-posting protections where feasible, foreign keys, and concurrency controls.
- Current process-local storage is demonstrative only; it must not be represented as durable or production-safe.
- Exposure, reserves, and payout eligibility remain separate implementation phases built on top of the ledger.

## Verification required before calling the financial core complete

- Unit tests for supported currency precision, rounding, currency mismatch, journal balancing by currency, and compensating reversals.
- PostgreSQL integration tests for migrations, atomic idempotency, duplicate/replayed requests, and concurrent posting/payout reservations.
- Failure tests for restart, transaction rollback, and ambiguous provider outcomes.
