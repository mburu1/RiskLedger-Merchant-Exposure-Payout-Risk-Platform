# Architecture overview

RiskLedger is an educational simulation using synthetic data only. The first implementation includes a .NET 10 modular solution, Angular shell, PostgreSQL Compose service, and a process-local ledger adapter. The adapter is non-durable and is not production-grade accounting storage.

## Dependency direction
- API -> Application + Infrastructure
- Infrastructure -> Application + Domain
- Application -> Domain
- Domain -> no framework or infrastructure dependencies
- Angular must not calculate authoritative financial balances.

## Planned modules
Organizer verification; bookings and supplier obligations; exposure; versioned risk policy; double-entry ledger; reserves; payout eligibility; simulated provider; disputes; reconciliation; audit; transactional outbox and workers.

## Financial invariants
1. Posted journal entries have at least two lines and balance exactly within one currency.
2. Each line has a non-negative debit or credit, but not both and not neither.
3. Posted records are immutable through the domain API; corrections use compensating entries.
4. An idempotency key cannot be reused for a different command.
5. Exposure, reserves, and payout availability remain separate measures.
6. Risk outcomes retain policy version, input snapshot and reason codes when implemented.
7. A risk score alone must never authorize a payout.
8. No real payment endpoints or real customer data are used.

## Illustrative posting
A synthetic KES 10,000 receipt is a debit to processor clearing (asset) and a credit to merchant funds payable (liability). This is illustrative; production accounting policy requires domain review.

## Next increments
1. PostgreSQL transactions, migrations, unique idempotency constraints and concurrency tests.
2. Account chart validation, durable immutable journal persistence and compensating reversals.
3. Organizer, trip, booking and supplier-obligation lifecycles.
4. Explainable exposure snapshots and deterministic versioned risk policy.
5. Reserve lifecycle and payout eligibility.
6. Outbox, reconciliation, authorization and Angular operational workflows.
