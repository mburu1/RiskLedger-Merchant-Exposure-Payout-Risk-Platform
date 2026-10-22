# Phase 1 local development

## Prerequisites
- .NET 10 SDK
- Node.js 22 and npm
- Docker Desktop with Docker Compose

## Start PostgreSQL (PowerShell, repository root)
```powershell
docker compose -f .\infrastructure\compose\compose.yaml up -d
docker compose -f .\infrastructure\compose\compose.yaml ps
docker compose -f .\infrastructure\compose\compose.yaml exec postgres pg_isready -U riskledger -d riskledger
```

## Run the API
```powershell
dotnet restore .\backend\RiskLedger.sln
dotnet build .\backend\RiskLedger.sln
$env:ASPNETCORE_ENVIRONMENT = "Development"
dotnet run --project .\backend\src\RiskLedger.Api\RiskLedger.Api.csproj --urls http://localhost:5080
```

Open Scalar at http://localhost:5080/scalar/v1 and OpenAPI JSON at http://localhost:5080/openapi/v1.json. Liveness is http://localhost:5080/health/live; readiness is http://localhost:5080/health/ready. Readiness is unhealthy if PostgreSQL cannot be reached.

## Run Angular
In a second terminal:
```powershell
Set-Location .\frontend\riskledger-web
npm ci
npm run lint
npm run build
npm start
```

Angular runs at http://localhost:4200. The demo ledger adapter remains in-memory in Phase 1; PostgreSQL is wired for connection/readiness and the EF Core persistence boundary. Durable ledger writes and migrations are explicitly deferred to the persistence phase.

## Shutdown
```powershell
docker compose -f .\infrastructure\compose\compose.yaml down
```
