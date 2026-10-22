# ADR 0001: Start with a modular monolith

- Status: Accepted
- Date: 2026-10-22

## Context
The project needs strong domain boundaries without distributed-systems complexity before financial invariants are stable.

## Decision
Use separate Domain, Application, Infrastructure and API projects. Keep domain behavior independent of ASP.NET Core, EF Core, databases, queues and provider SDKs. Add workers and broker integration only after transactional persistence and an outbox exist.

## Consequences
Local setup and CI remain approachable. Boundaries can be tested and extracted if justified. The in-memory ledger adapter is a stepping stone only; PostgreSQL-backed atomic posting is required before financial workflows.
