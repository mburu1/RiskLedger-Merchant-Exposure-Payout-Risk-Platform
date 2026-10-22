# RiskLedger — Merchant Exposure & Payout Risk Platform

> A portfolio-grade financial risk engineering platform built with C# / ASP.NET Core and Angular / TypeScript. RiskLedger models merchant-of-record exposure across future-dated bookings, supplier obligations, reserves, disputes, underwriting decisions, and payout eligibility.

**Purpose:** Demonstrate the engineering principles behind systems that make explainable, auditable decisions about financial exposure—not merely build a payments dashboard.

> **Portfolio / simulation disclaimer:** RiskLedger is an educational reference implementation. It does not move real money, connect to live payment rails, or provide production underwriting decisions. Use synthetic data and simulated providers only.

---

## 1. Product vision

Travel organizers can accept bookings today for trips departing months or years later. The platform may remain financially exposed until the trip happens, suppliers are paid, refunds and disputes settle, and obligations are reconciled.

RiskLedger gives Payment Operations and Risk teams one auditable place to answer:

- How much future supplier obligation exists for each organizer and trip?
- How much of that obligation is covered by available funds and existing reserves?
- Why was an organizer approved, restricted, or sent for manual review?
- How much can be paid out without violating reserve and risk policies?
- Which disputes, verification gaps, or changes require a reassessment?
- Can every balance and decision be explained from a traceable history?

### Product principles

1. **Financial correctness before features.** Enforce invariants in the domain and database; do not rely on UI validation.
2. **Explainable decisions.** Persist policy version, inputs, outcome, reason codes, and timestamps for each risk decision.
3. **Auditable money records.** Posted ledger entries are immutable; corrections use compensating entries.
4. **Idempotent commands.** Retries must not duplicate ledger postings, reserve movements, or payout requests.
5. **Least privilege.** Authorize sensitive actions on the server and record operator overrides.
6. **Operational ownership.** Build reconciliation, observability, recovery procedures, and incident runbooks—not just happy paths.
7. **Modular first.** Begin as a modular monolith and split deployable services only when a concrete operational need justifies it.

---

## 2. Proposed technology stack

| Area | Technology | Initial approach |
|---|---|---|
| Backend | C#, ASP.NET Core Web API on a supported .NET LTS release | REST API with explicit domain/application boundaries |
| Frontend | Angular + TypeScript | Feature-based operations console |
| API contracts | OpenAPI + Scalar | Versioned, documented API surface |
| Primary database | PostgreSQL | Relational source of truth for core financial records |
| Data access | EF Core | Migrations, explicit transaction boundaries, concurrency handling |
| Messaging | RabbitMQ | Asynchronous domain integration after the transactional outbox is implemented |
| Background processing | .NET Worker Service | Reconciliation, scheduled reassessment, reserve review |
| Authentication | OpenID Connect/OAuth 2.0 via a suitable identity provider | Server-side authorization; JWT validation where applicable |
| Testing | xUnit, ASP.NET Core integration tests, Testcontainers, architecture/contract tests | Unit, integration, concurrency, and failure-path coverage |
| Observability | OpenTelemetry, structured logs, metrics, traces | Correlation IDs and sensitive-data-safe telemetry |
| Containers | Docker Compose | Repeatable local development |
| CI/CD | GitHub Actions | Restore, format/lint, build, test, migration checks, security checks |
| Deployment | Kubernetes manifests later | Add after the local containerized system is reliable |
| Optional analytics | PostgreSQL views/materialized views initially | Add a warehouse only if scale and reporting needs require it |
| Optional AI | Provider-neutral LLM adapter for case summaries | Advisory only; never the authority for money movement |

### Technology constraints

- Start with **PostgreSQL only**. Add MongoDB only if a real document-oriented requirement emerges; do not add infrastructure for appearance.
- Start with a **modular monolith**, not a collection of premature microservices.
- Use `decimal` for monetary arithmetic in C# and an explicit currency/scale policy. Never use `float` or `double` for money.
- Store monetary values with a documented precision and currency. Do not assume every currency has two minor-unit digits.
- Do not use a floating-point risk score as the sole decision or payout authorization mechanism.
- Use synthetic fixtures and a simulated payment provider. No real cardholder data or live financial credentials.

---

## 3. Scope and core capabilities

### MVP modules

1. **Organizer & verification**
   - Organizer profile and lifecycle.
   - Verification status and evidence references (synthetic only).
   - Status-change history and reassessment triggers.

2. **Bookings & future obligations**
   - Trips, bookings, departure dates, and supplier obligations.
   - Obligation lifecycle and settlement status.
   - Exposure snapshots with reproducible calculation inputs.

3. **Risk policy engine**
   - Versioned deterministic rules.
   - Outcomes: `Approve`, `Review`, `Restrict` (or a clearly documented equivalent).
   - Reason codes and decision explanations.
   - Manual review queue and controlled overrides.

4. **Financial ledger**
   - Chart of accounts.
   - Journal entries and debit/credit lines.
   - Each posted journal entry must balance per currency.
   - Immutable posted entries and compensating reversals.
   - Idempotency keys for posting commands.

5. **Reserve management**
   - Reserve policies and policy versions.
   - Required reserve calculation based on modeled exposure.
   - Reserve allocations/releases with reasons and audit history.
   - Scheduled reevaluation when obligations or risk state change.

6. **Payout eligibility**
   - Available-balance calculation derived from ledger records.
   - Reserve, verification, risk, and payout-state checks.
   - Idempotent payout request creation.
   - Simulated provider adapter and explicit payout state machine.
   - Never treat a low risk score as sufficient authorization.

7. **Disputes & operations**
   - Dispute lifecycle and evidence checklist.
   - Organizer risk timeline.
   - Searchable operational dashboard.
   - Audit log for policy changes, decisions, overrides, and payout actions.

8. **Reconciliation & reliability**
   - Compare simulated provider statements with internal records.
   - Detect missing, duplicated, delayed, or mismatched entries.
   - Safe retry and replay procedures.
   - Dead-letter handling and operational alerts for asynchronous processing.

### Explicitly out of scope for the first release

- Real payment processing or live payout execution.
- Storing PAN, CVV, bank credentials, or real KYC documents.
- Autonomous LLM-based underwriting or payout decisions.
- Multi-region active-active deployment.
- A full data warehouse before the operational model is stable.

---

## 4. Repository structure

This tree is the intended target. Create folders as their phase begins; do not add empty projects simply to make the tree look complete.

```text
RiskLedger/
├── README.md
├── LICENSE
├── .editorconfig
├── .gitignore
├── global.json
├── Directory.Build.props
├── Directory.Packages.props
├── RiskLedger.sln
│
├── backend/
│   ├── src/
│   │   ├── RiskLedger.Api/                 # HTTP endpoints, auth, composition root
│   │   ├── RiskLedger.Application/         # Use cases, ports, command/query handlers
│   │   ├── RiskLedger.Domain/              # Entities, value objects, invariants, domain events
│   │   ├── RiskLedger.Infrastructure/      # EF Core, PostgreSQL, messaging, external adapters
│   │   ├── RiskLedger.Contracts/            # API request/response contracts
│   │   ├── RiskLedger.Ledger/               # Ledger domain module
│   │   ├── RiskLedger.RiskEngine/           # Policies, assessments, reason codes
│   │   ├── RiskLedger.Exposure/             # Forward obligations and exposure snapshots
│   │   ├── RiskLedger.Reserves/             # Reserve policy and movements
│   │   ├── RiskLedger.Payouts/              # Eligibility and payout state machine
│   │   ├── RiskLedger.Disputes/             # Dispute workflow
│   │   └── RiskLedger.Workers/              # Scheduled and asynchronous jobs
│   ├── tests/
│   │   ├── RiskLedger.UnitTests/
│   │   ├── RiskLedger.IntegrationTests/
│   │   ├── RiskLedger.ArchitectureTests/
│   │   └── RiskLedger.ContractTests/
│   └── tools/
│       └── RiskLedger.Seeder/               # Deterministic synthetic data
│
├── frontend/
│   └── riskledger-web/
│       ├── src/app/
│       │   ├── core/                        # Auth, API clients, interceptors
│       │   ├── shared/                      # Reusable UI and validators
│       │   ├── features/
│       │   │   ├── dashboard/
│       │   │   ├── organizers/
│       │   │   ├── risk-assessments/
│       │   │   ├── exposure/
│       │   │   ├── reserves/
│       │   │   ├── payouts/
│       │   │   ├── disputes/
│       │   │   ├── ledger/
│       │   │   └── audit/
│       │   └── app.routes.ts
│       └── ...
│
├── infrastructure/
│   ├── docker/
│   ├── compose/
│   │   └── compose.yaml
│   ├── postgres/
│   ├── rabbitmq/
│   └── kubernetes/                          # Later phase
│
├── docs/
│   ├── product/
│   │   ├── problem-statement.md
│   │   ├── personas-and-workflows.md
│   │   └── scope-and-non-goals.md
│   ├── architecture/
│   │   ├── architecture-overview.md
│   │   ├── decisions/                       # ADRs
│   │   └── threat-model.md
│   ├── domain/
│   │   ├── glossary.md
│   │   ├── invariants.md
│   │   └── risk-policy.md
│   ├── ooad/
│   ├── uml/
│   ├── database/
│   │   ├── erd.md
│   │   └── migrations-and-recovery.md
│   ├── api/
│   ├── security/
│   ├── testing/
│   └── operations/
│       ├── runbooks/
│       ├── reconciliation.md
│       └── incident-response.md
│
├── scripts/
│   ├── dev-up.ps1
│   ├── dev-down.ps1
│   └── verify.ps1
│
└── .github/
    ├── workflows/
    │   ├── backend-ci.yml
    │   ├── frontend-ci.yml
    │   └── security.yml
    └── PULL_REQUEST_TEMPLATE.md
```

### Dependency direction

Keep the dependency graph deliberate:

```text
RiskLedger.Api ──────────────┐
RiskLedger.Workers ──────────┤
                             v
                    Application
                         |
                         v
                       Domain

Infrastructure ───────> Application + Domain
Contracts ─────────────> API boundary only
```

The domain must not depend on EF Core, ASP.NET Core, Angular, the message broker, or an external provider SDK. The application layer orchestrates use cases through interfaces; infrastructure implements those interfaces.

For the initial modular monolith, modules can be projects or clearly separated namespaces/folders. Avoid circular dependencies and avoid making every module a network service.

---

## 5. Domain model and financial invariants

### Core domain concepts

| Concept | Responsibility |
|---|---|
| `Organizer` | Merchant profile and lifecycle |
| `VerificationCase` | Verification status and review history |
| `Trip` | Trip dates and organizer ownership |
| `Booking` | Customer booking and payment state (synthetic) |
| `SupplierObligation` | Expected or committed supplier liability |
| `ExposureSnapshot` | Time-stamped, explainable exposure calculation |
| `RiskPolicy` / `RiskPolicyVersion` | Versioned decision rules |
| `RiskAssessment` | Inputs, outcome, reason codes, policy version |
| `LedgerAccount` | Account in the chart of accounts |
| `JournalEntry` / `JournalLine` | Immutable posted accounting movement |
| `ReserveAllocation` | Amount restricted for a documented purpose |
| `PayoutRequest` | Requested transfer and current state |
| `Dispute` | Chargeback/dispute lifecycle |
| `AuditRecord` | Actor, action, target, timestamp, correlation ID |

### Invariants that must be enforced and tested

- A posted journal entry balances: total debits equal total credits **for each currency**.
- Posted ledger entries cannot be edited or deleted through normal application operations.
- A correction is represented by a compensating journal entry linked to the original.
- Every money-moving command has an idempotency strategy.
- Concurrent requests cannot spend the same available balance twice.
- Payout eligibility is evaluated from authoritative state, not frontend values.
- A payout cannot exceed the amount permitted by available funds, holds, and applicable policy.
- Reserve movements have an explicit source, destination, reason, and audit trail.
- Every risk decision stores its policy version and reason codes.
- All financial calculations have a documented rounding and currency policy.
- External events can be delivered more than once without duplicating business effects.
- A reconciliation mismatch is visible and actionable; it is not silently overwritten.

**Accounting design note:** Do not model the ledger as a single mutable `Balance` column. Balances can be derived or maintained as a carefully controlled projection, but the journal remains the accounting source of truth. Document the accounting treatment of pending, available, reserved, and payable amounts before implementing posting rules.

### Exposure model (illustrative)

A useful starting calculation is:

```text
uncovered_supplier_exposure =
    max(0, eligible_supplier_obligations
           - eligible_funds_allocated_to_cover_those_obligations)
```

This is not a complete underwriting model. Define precisely which obligations and funds qualify, how currencies are converted, how timing is handled, and how double counting is prevented. Keep gross exposure, covered exposure, reserve requirements, and payout availability as separate concepts.

---

## 6. API design starter

Use resource-oriented endpoints and explicit commands for state-changing actions. Names below are illustrative; finalize request/response contracts in the API phase.

| Method | Endpoint | Purpose |
|---|---|---|
| `GET` | `/api/v1/organizers` | Search and paginate organizers |
| `GET` | `/api/v1/organizers/{id}` | Organizer detail and risk summary |
| `GET` | `/api/v1/organizers/{id}/exposure` | Exposure breakdown and calculation timestamp |
| `GET` | `/api/v1/organizers/{id}/risk-assessments` | Assessment history |
| `POST` | `/api/v1/organizers/{id}/risk-assessments` | Request a new assessment |
| `GET` | `/api/v1/reserves` | Search reserve allocations |
| `POST` | `/api/v1/reserves/{id}/release-requests` | Request a controlled release |
| `GET` | `/api/v1/payouts` | Search payout requests |
| `POST` | `/api/v1/payouts` | Create an idempotent payout request |
| `POST` | `/api/v1/payouts/{id}/cancel` | Cancel if the state machine permits |
| `GET` | `/api/v1/disputes` | Search disputes |
| `GET` | `/api/v1/ledger/accounts/{id}/entries` | Read ledger history |
| `GET` | `/api/v1/audit` | Search authorized audit records |
| `GET` | `/health/live` | Liveness probe |
| `GET` | `/health/ready` | Readiness probe |

### API requirements

- Use pagination, sorting allowlists, and bounded filters.
- Validate input at the API boundary and enforce invariants in the domain/application layers.
- Use authorization policies for operator roles and high-risk actions.
- Use an idempotency key for payout creation and other commands that must not execute twice.
- Return consistent problem-details responses without leaking internal exceptions.
- Add correlation IDs and trace context.
- Never log secrets, access tokens, bank details, or sensitive verification evidence.
- Use optimistic concurrency or transactional locking where concurrent changes can invalidate a decision.
- Require a reason and appropriate approval for manual overrides.
- Do not expose internal database entities directly as API contracts.

---

## 7. Implementation phases and acceptance criteria

Build vertically: a phase is complete only when its behavior, tests, documentation, and verification steps are present.

### Phase 0 — Product framing and engineering foundation

**Implement**
- Problem statement, users, workflows, non-goals, glossary.
- Architecture overview and initial architecture decision records (ADRs).
- Repository conventions, `.editorconfig`, SDK pinning, dependency management.
- Threat model and local development instructions.
- A minimal CI workflow that builds and tests the initial skeleton.

**Acceptance criteria**
- A new developer can follow the README to restore and build the solution.
- Architecture boundaries and financial assumptions are documented.
- CI runs on a clean checkout.
- No secrets or personal data are committed.

### Phase 1 — Solution skeleton and local environment

**Implement**
- .NET solution and projects.
- Angular application with routing, linting, and a basic application shell.
- PostgreSQL via Docker Compose.
- Health/readiness endpoints and configuration validation.
- OpenAPI/Scalar.
- Local synthetic data seeding.

**Acceptance criteria**
- Backend and frontend start locally using documented commands.
- Database health is checked before dependent services are considered ready.
- No production secrets are required for local development.
- CI builds both applications.

### Phase 2 — Domain model and database

**Implement**
- Organizers, trips, bookings, supplier obligations, disputes, and verification cases.
- EF Core configurations and migrations.
- Explicit constraints, indexes, audit metadata, and concurrency strategy.
- ERD and domain glossary.

**Acceptance criteria**
- Migrations create a clean database from scratch.
- A seeded dataset is deterministic and contains no real personal data.
- Invalid state transitions and invalid references are rejected.
- Database constraints protect critical invariants where feasible.

### Phase 3 — Ledger foundation

**Implement**
- Chart of accounts, journal entries, journal lines, and posting service.
- Currency-aware money value object and rounding rules.
- Idempotent posting command.
- Reversal/compensating-entry workflow.
- Ledger history endpoint and tests.

**Acceptance criteria**
- Every posted journal entry balances by currency.
- Posted entries cannot be modified or deleted through normal commands.
- Duplicate requests do not create duplicate postings.
- Concurrent posting tests verify consistency.
- Reversals preserve the original record and create a traceable correction.

### Phase 4 — Exposure calculation

**Implement**
- Supplier obligation lifecycle and eligibility rules.
- Exposure calculator with explicit input snapshot.
- Organizer/trip-level exposure summaries.
- Calculation version and timestamp.
- Tests for partial settlement, future dates, currencies, missing data, and duplicate inputs.

**Acceptance criteria**
- Every total can be traced to the underlying eligible records.
- Gross obligations, covered funds, uncovered exposure, reserves, and payout availability are distinct.
- Recalculation is deterministic for the same inputs and version.
- Rounding and currency-conversion assumptions are documented.

### Phase 5 — Risk and underwriting rules

**Implement**
- Versioned deterministic policy model.
- Risk assessment with decision, reason codes, policy version, and input snapshot.
- Review queue and assessment history.
- Authorized manual override with reason, actor, timestamp, and audit record.

**Acceptance criteria**
- The same policy version and inputs produce the same decision.
- Every outcome is explainable without an LLM.
- Policy edits create new versions rather than rewriting historical decisions.
- Missing or stale verification data leads to a documented safe outcome.
- Unauthorized users cannot change policies or override decisions.

### Phase 6 — Reserve management

**Implement**
- Reserve policies and reserve allocations.
- Required-reserve calculation using eligible exposure inputs.
- Reserve hold/release commands and lifecycle.
- Scheduled reevaluation triggered by relevant changes.

**Acceptance criteria**
- Reserve movements are auditable and idempotent.
- Releases cannot violate the applicable reserve policy or available-funds constraints.
- Recalculation does not duplicate movements.
- Manual overrides require authorization and a reason.
- Tests cover races between recalculation, release, and payout requests.

### Phase 7 — Payout eligibility and simulated provider

**Implement**
- Payout state machine.
- Eligibility policy for verification, available funds, reserves, and risk restrictions.
- Idempotent payout request.
- Simulated provider adapter with success, decline, timeout, and ambiguous-outcome scenarios.
- Safe retry and reconciliation of unknown outcomes.

**Acceptance criteria**
- A low risk score alone cannot authorize a payout.
- Concurrent requests cannot overspend available funds.
- Retrying a timed-out request cannot blindly initiate a duplicate transfer.
- State transitions are validated and audited.
- No real payment endpoint or credentials are used.

### Phase 8 — Messaging and background work

**Implement**
- Transactional outbox and idempotent consumers.
- RabbitMQ integration.
- Worker for reassessment/reconciliation tasks.
- Retry policy, dead-letter handling, and failure metrics.
- Event contracts and versioning conventions.

**Acceptance criteria**
- Database state and outbox record commit atomically.
- Duplicate event delivery does not duplicate business effects.
- Poison messages are observable and recoverable.
- Retry behavior is bounded and documented.
- Integration tests cover broker interruption and worker restart.

### Phase 9 — Angular operations console

**Implement**
- Dashboard for exposure, reserves, disputes, and payout state.
- Organizer detail and risk timeline.
- Risk review queue and decision explanation.
- Reserve and payout workflows with confirmation and reason capture.
- Ledger and audit history views.
- Accessible loading, empty, validation, and error states.

**Acceptance criteria**
- UI data comes from the API; it never calculates authoritative balances.
- All sensitive actions are authorized by the backend.
- Tables support server-side filtering and pagination.
- High-risk actions show the consequences and require explicit confirmation.
- Automated frontend tests cover critical workflows.

### Phase 10 — Reconciliation, security, and reliability

**Implement**
- Simulated provider statement import.
- Reconciliation reports and discrepancy lifecycle.
- Authentication, role-based/permission-based authorization, and secret management.
- Rate limiting and secure configuration.
- Structured logs, metrics, traces, dashboards, and alerts.
- Threat-model review and dependency scanning.

**Acceptance criteria**
- Reconciliation detects missing, duplicate, and mismatched transactions.
- Financial mismatches are never silently overwritten.
- Logs redact sensitive fields.
- Authorization tests cover privileged operations.
- A documented recovery procedure exists for failed payouts and stuck messages.

### Phase 11 — Production-style delivery

**Implement**
- CI for backend and frontend, plus integration test services.
- Docker images and reproducible Compose environment.
- Migration validation and safe deployment notes.
- Kubernetes manifests only after containerized behavior is stable.
- Load tests for key queries and concurrency-sensitive commands.
- Operations runbooks and incident postmortem template.

**Acceptance criteria**
- A clean CI run builds and tests all supported components.
- Health checks and graceful shutdown are verified.
- Critical-path latency and throughput are measured, not guessed.
- Backup/restore and reconciliation recovery are exercised.
- README and architecture diagrams match the implemented system.

### Phase 12 — Optional AI-assisted investigation

**Implement**
- Provider-neutral AI adapter behind an application interface.
- Case summaries using authorized, minimized data.
- Evidence citations back to source records.
- Human review and feedback capture.

**Acceptance criteria**
- AI output is advisory and cannot post ledger entries or authorize payouts.
- Sensitive data is minimized and redacted.
- Unsupported claims are visibly distinguished from source facts.
- The system remains usable when the LLM provider is unavailable.
- Evaluation tests cover hallucinations, prompt injection, and access-boundary failures.

---

## 8. Testing strategy

Financial correctness needs more than controller unit tests.

### Unit tests
- Money arithmetic, currency validation, rounding.
- Ledger balancing and permitted account transitions.
- Exposure and reserve calculations.
- Risk policy evaluation and reason codes.
- Payout state transitions.

### Integration tests
- PostgreSQL constraints and migrations.
- Transaction rollback behavior.
- Idempotency under retries.
- Concurrency when multiple payouts compete for funds.
- Outbox atomicity and consumer deduplication.
- Authentication and authorization policies.

### Contract and end-to-end tests
- OpenAPI/API contract checks.
- Angular critical workflows against the API.
- Simulated provider success, decline, timeout, and ambiguous result.
- Reconciliation discrepancy resolution.

### Property/invariant tests
Where practical, generate many valid and invalid combinations to check that:
- Posted journal entries remain balanced.
- Available funds never become negative unless an explicitly modeled, authorized account policy permits it.
- Duplicate commands do not duplicate financial effects.
- A reversal does not erase original history.
- Reserve release never exceeds the releasable amount.
- Risk decisions retain their original policy version and inputs.

### Failure testing
- Database unavailable during startup or processing.
- Broker unavailable after a database transaction.
- Worker crashes after receiving an event.
- Provider timeout with unknown outcome.
- Duplicate or out-of-order messages.
- Concurrent reserve release and payout creation.

---

## 9. Security and operational standards

- Use synthetic data only.
- Store configuration in environment variables or a secrets manager; commit only safe examples.
- Enforce authentication and authorization on every protected endpoint.
- Separate read, policy-management, manual-review, reserve-release, and payout permissions.
- Require strong controls for overrides and privileged actions.
- Do not log tokens, passwords, bank data, or sensitive verification content.
- Record actor, action, target, timestamp, reason, correlation ID, and relevant before/after references for audit-sensitive actions.
- Apply least privilege to database, broker, and service identities.
- Use bounded retries, timeouts, cancellation, and circuit-breaking where appropriate.
- Define alert thresholds from measured behavior and risk tolerance.
- Document backup, restore, reconciliation, and incident procedures.

This project is a portfolio simulation, not a compliance certification. A real financial system requires domain review, security review, legal/compliance input, and operational controls appropriate to its deployment.

---

## 10. Documentation and diagrams

Maintain documentation alongside implementation. Recommended diagrams:

1. System context and container architecture.
2. Dependency/layer diagram.
3. Organizer onboarding and verification sequence.
4. Risk assessment activity diagram.
5. Exposure calculation sequence.
6. Ledger posting and reversal sequence.
7. Reserve allocation/release sequence.
8. Payout eligibility state machine.
9. Payout timeout and reconciliation sequence.
10. Transactional outbox and consumer retry sequence.
11. ERD and key constraints.
12. Threat model and trust boundaries.

Use Mermaid `.mmd` source files under `docs/uml/` or `docs/architecture/diagrams/`. Validate every diagram in CI with a Mermaid parser/render step, and keep diagrams aligned with the actual implementation.

### Architecture Decision Records (ADRs)

Write short ADRs for decisions such as:
- Modular monolith before microservices.
- PostgreSQL as the financial source of truth.
- Double-entry ledger design.
- Money precision and currency policy.
- Transactional outbox and delivery semantics.
- Idempotency strategy.
- Reserve and exposure definitions.
- Authorization model.
- When (and whether) to introduce Kafka, MongoDB, or Kubernetes.

---

## 11. Local development and verification

Exact commands depend on the installed .NET SDK and Angular CLI versions. Pin supported versions in `global.json` and the frontend package manager lockfile, and keep the commands below aligned with the actual solution.

### Backend

```powershell
# From repository root
dotnet --info
dotnet restore .\backend\RiskLedger.sln
dotnet build .\backend\RiskLedger.sln --no-restore
dotnet test .\backend\RiskLedger.sln --no-build
```

If the solution file is created in the repository root instead, update these paths consistently.

### Frontend

```powershell
cd .\frontend\riskledger-web
npm ci
npm run lint
npm test -- --watch=false
npm run build
```

Angular project scripts vary by version; ensure the `lint` and `test` scripts exist in `package.json` and adjust commands to the selected test runner.

### Infrastructure

```powershell
docker compose -f .\infrastructure\compose\compose.yaml config
docker compose -f .\infrastructure\compose\compose.yaml up -d
docker compose -f .\infrastructure\compose\compose.yaml ps
docker compose -f .\infrastructure\compose\compose.yaml logs --tail 100
docker compose -f .\infrastructure\compose\compose.yaml down
```

Never run destructive database reset commands against an environment containing data you need.

### Completion checklist

- [ ] Backend restore/build/test pass from a clean checkout.
- [ ] Frontend install/lint/test/build pass from a clean checkout.
- [ ] Database migrations create a new database successfully.
- [ ] Ledger balancing and immutability invariants are tested.
- [ ] Idempotency and concurrent payout scenarios are tested.
- [ ] Exposure and reserve calculations are explainable.
- [ ] Risk decisions are versioned and auditable.
- [ ] Reconciliation detects mismatches.
- [ ] Logs and error responses do not leak sensitive information.
- [ ] CI verifies the supported build and test commands.
- [ ] Diagrams, API docs, and README reflect the actual implementation.
- [ ] Demo uses synthetic data and a simulated provider only.

---

## 12. Portfolio demo scenario

Seed one synthetic travel organizer with:
- Two future trips with different departure dates.
- Several bookings and supplier obligations.
- A verification state and versioned risk policy.
- Balanced ledger postings.
- An existing reserve and a pending payout.
- One simulated dispute.
- One simulated provider timeout to exercise reconciliation.

The demo should let a reviewer:
1. Open the organizer profile.
2. Inspect gross and uncovered exposure with the underlying records.
3. See the current risk decision and reason codes.
4. Inspect the reserve calculation and its history.
5. Submit a simulated payout request and observe eligibility checks.
6. Trigger a duplicate request to verify idempotency.
7. Inspect ledger entries and audit history.
8. Resolve a simulated reconciliation mismatch.
9. Review logs/traces for the end-to-end operation.

Use a concise walkthrough video or screenshots in the GitHub README only after the flows work reliably.

---

## 13. Definition of done

A feature is complete only when:
- Domain behavior and invariants are defined.
- API contract and authorization are implemented.
- Database migration and rollback/recovery considerations are documented.
- Unit and relevant integration tests pass.
- Idempotency/concurrency behavior is addressed where money or state is affected.
- Logs, metrics, errors, and audit events are appropriate.
- UI states are accessible and handle failures.
- Documentation and diagrams are updated.
- CI passes on a clean checkout.

---

## 14. Suggested GitHub issues

Create milestone labels such as `phase-00-foundation`, `phase-01-skeleton`, and so on. Keep each issue small enough to review.

Suggested first issues:
1. Define glossary and financial invariants.
2. Record ADR for modular monolith and PostgreSQL.
3. Scaffold .NET solution and enforce dependency direction.
4. Scaffold Angular app and CI.
5. Add PostgreSQL Compose service and health checks.
6. Add EF Core migrations and deterministic synthetic seed data.
7. Implement Money value object and currency policy.
8. Implement balanced ledger posting and idempotency tests.
9. Implement exposure calculation with explainable snapshots.
10. Implement versioned risk policies and reason codes.

**Start with Phase 0.** Do not implement reserves or payouts until the financial domain definitions and ledger invariants are explicit and tested.

---

## License

Choose a license before publishing. For a public portfolio repository, MIT is a common option, but confirm that it matches your intended reuse and attribution requirements.
