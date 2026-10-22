# Local development

## Prerequisites
- .NET 10 SDK
- Node.js 22 LTS and npm
- Docker Desktop with Compose

## Backend (PowerShell)
From the repository root:

```powershell
dotnet restore .\backend\RiskLedger.sln
dotnet build .\backend\RiskLedger.sln
dotnet test .\backend\RiskLedger.sln
dotnet run --project .\backend\src\RiskLedger.Api\RiskLedger.Api.csproj --urls http://localhost:5080
```

Health endpoints are /health/live and /health/ready. OpenAPI is /openapi/v1.json in Development. The current ledger adapter is process-local; restarting the API clears entries.

Create a synthetic posting:

```powershell
$body = @{ idempotencyKey = "demo-booking-001"; amount = 10000; currency = "KES" } | ConvertTo-Json
Invoke-RestMethod -Method Post -Uri http://localhost:5080/api/v1/ledger/demo-posting -ContentType "application/json" -Body $body
Invoke-RestMethod -Uri http://localhost:5080/api/v1/ledger/entries
```

## Frontend

```powershell
Set-Location .\frontend\riskledger-web
npm install
npm start
```

## PostgreSQL

```powershell
Copy-Item .env.example .env
docker compose --env-file .env -f .\infrastructure\compose\compose.yaml up -d
docker compose -f .\infrastructure\compose\compose.yaml ps
```

PostgreSQL is provisioned for the persistence milestone but is not yet connected to the API. Never commit real secrets or use the sample password outside an isolated local environment.
