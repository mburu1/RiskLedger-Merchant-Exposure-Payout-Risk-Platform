# RiskLedger — Merchant Exposure & Payout Risk Platform

A portfolio-grade financial risk engineering simulation built with C# / ASP.NET Core 10, Angular / TypeScript, and PostgreSQL as the planned durable source of truth.

> Simulation only: no real money movement, live payment rails, or production underwriting. Synthetic data only.

## Implemented first slice
- .NET 10 modular solution: Domain, Application, Infrastructure, API and unit tests.
- Currency-aware Money and double-entry journal validation.
- Idempotent ledger posting behavior in a process-local development adapter.
- ASP.NET Core health probes, OpenAPI in Development, ledger demo POST and ledger history GET.
- Angular operations landing screen.
- PostgreSQL Compose service, build conventions and GitHub Actions CI.

**Important limitation:** the ledger is currently held in memory and is lost when the API restarts. PostgreSQL is provisioned but not yet wired into the API. This is not durable accounting storage.

## Run locally (PowerShell)
Prerequisites: .NET 10 SDK, Node.js 22+, npm, Docker Desktop with Compose.

```powershell
dotnet restore .\backend\RiskLedger.sln
dotnet build .\backend\RiskLedger.sln
dotnet test .\backend\RiskLedger.sln
dotnet run --project .\backend\src\RiskLedger.Api\RiskLedger.Api.csproj --urls http://localhost:5080
```

In another terminal:

```powershell
Set-Location .\frontend\riskledger-web
npm install
npm start
```

API: http://localhost:5080. Angular: http://localhost:4200. Health: /health/live and /health/ready. OpenAPI: /openapi/v1.json in Development.

## Create a synthetic KES posting

```powershell
$body = @{ idempotencyKey = "demo-booking-001"; amount = 10000; currency = "KES" } | ConvertTo-Json
Invoke-RestMethod -Method Post -Uri http://localhost:5080/api/v1/ledger/demo-posting -ContentType "application/json" -Body $body
Invoke-RestMethod -Uri http://localhost:5080/api/v1/ledger/entries
```

## PostgreSQL local service

```powershell
Copy-Item .env.example .env
docker compose --env-file .env -f .\infrastructure\compose\compose.yaml up -d
docker compose -f .\infrastructure\compose\compose.yaml ps
```

The API does not yet connect to PostgreSQL. Change the example password for anything beyond isolated local development; never commit secrets.

## Next critical milestone
Replace the in-memory adapter with PostgreSQL-backed atomic journal posting, migrations, unique idempotency constraints, concurrency tests and compensating-entry reversals. Then build organizer/booking obligations and explainable exposure before reserves or payouts.

See [RiskLedger_README.md](RiskLedger_README.md) for the full product vision and phased delivery plan.