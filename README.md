# RiskLedger — Merchant Exposure & Payout Risk Platform

A portfolio-grade financial risk engineering simulation built with C# / ASP.NET Core 10, Angular / TypeScript, and PostgreSQL.

> **Simulation only:** no real money movement, live payment rails, production underwriting, real KYC data, or real provider credentials. Use synthetic data and simulated providers only.

## Current implementation

The repository has an initial engineering foundation and a first ledger vertical slice:

- .NET 10 solution with Domain, Application, Infrastructure, API, and unit-test projects.
- Currency-aware `Money` value object using `decimal`, explicit supported-currency scale, input precision validation, and explicit midpoint-to-even rounding.
- Double-entry journal validation that balances each currency independently.
- Ledger account, journal posting, history, and compensating reversal API paths.
- PostgreSQL/EF Core/Npgsql integration and a PostgreSQL ledger adapter wired into the API.
- Liveness/readiness probes, development-only OpenAPI + Scalar, structured JSON logging, and centralized Problem Details exception handling.
- Angular operations shell, feature routing, and typed readiness view.
- Docker Compose PostgreSQL configuration and initial CI workflow.

This is an **in-progress implementation**, not a finished financial platform. The existence of a PostgreSQL adapter does not by itself prove migration correctness, atomic idempotency, concurrency safety, immutability, or recovery. Those guarantees must be demonstrated by database-backed tests and clean-checkout verification.

See [the implementation status and remaining roadmap](docs/implementation-status.md) for the current completion boundary and outstanding phases. The financial rules in [the domain invariants](docs/domain/invariants.md) are normative.

## Financial invariants

- Money uses `decimal`; currencies are explicit and never implicitly combined or converted.
- The current currency catalogue is KES, USD, EUR, GBP (2 minor-unit digits), JPY (0), and KWD (3).
- Each journal entry balances independently for every currency.
- Posted entries are immutable through ordinary commands; corrections use linked compensating entries.
- Posting idempotency must be enforced transactionally in durable storage; a key reused for a different request must be rejected.
- Gross obligations, covered exposure, required reserves, and available payout balance are distinct measures.
- Pending funds are not payout-available by default.
- Payout eligibility must evaluate authoritative settled funds, holds/reserves, verification, current risk restrictions, lifecycle, and idempotency—not merely a risk score.
- Ambiguous provider outcomes must be reconciled before retry; never blindly initiate a duplicate transfer.

## Run locally (PowerShell)

Prerequisites: .NET 10 SDK, Node.js 22+, npm, and Docker Desktop with Compose.

From the repository root:

```powershell
dotnet restore .\backend\RiskLedger.sln
dotnet build .\backend\RiskLedger.sln --configuration Release
dotnet test .\backend\RiskLedger.sln --configuration Release
```

Start PostgreSQL:

```powershell
Copy-Item .env.example .env
docker compose --env-file .env -f .\infrastructure\compose\compose.yaml up -d
docker compose --env-file .env -f .\infrastructure\compose\compose.yaml ps
```

Start the API in a separate terminal:

```powershell
$env:ASPNETCORE_ENVIRONMENT = "Development"
dotnet run --project .\backend\src\RiskLedger.Api\RiskLedger.Api.csproj --urls http://localhost:5080
```

Start the frontend in another terminal:

```powershell
Set-Location .\frontend\riskledger-web
npm install
npm start
```

Default local endpoints:

- API: `http://localhost:5080`
- Angular: `http://localhost:4200`
- Liveness: `/health/live`
- Readiness: `/health/ready`
- Scalar: `/scalar/v1` (Development only)
- OpenAPI: `/openapi/v1.json` (Development only)

Configure `window.__RISKLEDGER_API_URL__` before frontend bootstrap when the API is hosted elsewhere. The local database credentials are for isolated development only; never commit real secrets.

## Create a synthetic ledger posting

Use the demo endpoint only with synthetic data:

```powershell
$body = @{ idempotencyKey = "demo-booking-001"; amount = 10000; currency = "KES" } | ConvertTo-Json
Invoke-RestMethod -Method Post -Uri http://localhost:5080/api/v1/ledger/demo-posting -ContentType "application/json" -Body $body
Invoke-RestMethod -Uri http://localhost:5080/api/v1/ledger/entries
```

## Roadmap

The intended phases cover core domain persistence; exposure calculations; versioned risk decisions; reserve management; payout eligibility and a simulated provider; transactional outbox/RabbitMQ and workers; Angular operations workflows; reconciliation and security; delivery hardening; and optional advisory AI. Phase completion requires behavior, tests, documentation, and verification—not just folders or scaffolding.

The immediate priority is to verify PostgreSQL migrations and atomic/idempotent ledger posting under concurrency, then implement organizer/booking/supplier-obligation persistence and deterministic exposure calculations. Do not treat reserves or payouts as complete until the ledger and exposure foundations meet their acceptance criteria.

## License

See the repository license file for reuse terms.
