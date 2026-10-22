using Microsoft.EntityFrameworkCore;

namespace RiskLedger.Infrastructure;

/// <summary>Persistence boundary for RiskLedger. Domain entities are added with their owning phases.</summary>
public sealed class RiskLedgerDbContext(DbContextOptions<RiskLedgerDbContext> options) : DbContext(options)
{
}
