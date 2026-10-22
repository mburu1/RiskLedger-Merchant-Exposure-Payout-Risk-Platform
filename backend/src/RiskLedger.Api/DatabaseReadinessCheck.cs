using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using RiskLedger.Infrastructure;

public sealed class DatabaseReadinessCheck(RiskLedgerDbContext dbContext) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var canConnect = await dbContext.Database.CanConnectAsync(cancellationToken);
            return canConnect
                ? HealthCheckResult.Healthy("PostgreSQL accepts connections.")
                : HealthCheckResult.Unhealthy("PostgreSQL is not accepting connections.");
        }
        catch (Exception)
        {
            return HealthCheckResult.Unhealthy("PostgreSQL readiness check failed.");
        }
    }
}
