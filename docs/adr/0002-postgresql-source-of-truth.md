# ADR 0002: PostgreSQL is the intended financial source of truth

- Status: Accepted
- Date: 2026-10-22

## Context
Ledger postings, idempotency, reserves and payout requests require durable consistency, unique constraints and transactional behavior.

## Decision
Use PostgreSQL as the source of truth for financial records, with EF Core migrations and database constraints. Add a transactional outbox before broker delivery.

## Consequences
Compose provisions a local PostgreSQL service. The current API does not yet persist to PostgreSQL. Reserve and payout workflows are not complete until transactional and concurrency behavior are tested.
