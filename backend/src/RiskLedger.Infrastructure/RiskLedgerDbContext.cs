using Microsoft.EntityFrameworkCore;

namespace RiskLedger.Infrastructure;

public sealed class RiskLedgerDbContext(DbContextOptions<RiskLedgerDbContext> options) : DbContext(options)
{
    public DbSet<LedgerAccountRow> Accounts => Set<LedgerAccountRow>();
    public DbSet<JournalEntryRow> JournalEntries => Set<JournalEntryRow>();
    public DbSet<JournalLineRow> JournalLines => Set<JournalLineRow>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<LedgerAccountRow>(b =>
        {
            b.ToTable("ledger_accounts");
            b.HasKey(x => x.Code);
            b.Property(x => x.Code).HasMaxLength(64);
            b.Property(x => x.Name).HasMaxLength(160).IsRequired();
            b.Property(x => x.Currency).HasMaxLength(3).IsRequired();
            b.Property(x => x.AccountType).HasMaxLength(16).IsRequired();
            b.HasIndex(x => new { x.Code, x.Currency }).IsUnique();
        });
        modelBuilder.Entity<JournalEntryRow>(b =>
        {
            b.ToTable("journal_entries");
            b.HasKey(x => x.Id);
            b.Property(x => x.IdempotencyKey).HasMaxLength(200).IsRequired();
            b.Property(x => x.Description).HasMaxLength(500).IsRequired();
            b.Property(x => x.PayloadHash).HasMaxLength(64).IsRequired();
            b.HasIndex(x => x.IdempotencyKey).IsUnique();
            b.HasIndex(x => x.PostedAt);
            b.HasIndex(x => x.ReversalOfEntryId).IsUnique().HasFilter("\"ReversalOfEntryId\" IS NOT NULL");
            b.HasMany(x => x.Lines).WithOne().HasForeignKey(x => x.JournalEntryId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<JournalLineRow>(b =>
        {
            b.ToTable("journal_lines");
            b.HasKey(x => x.Id);
            b.Property(x => x.AccountCode).HasMaxLength(64).IsRequired();
            b.Property(x => x.Currency).HasMaxLength(3).IsRequired();
            b.Property(x => x.Debit).HasPrecision(20, 3);
            b.Property(x => x.Credit).HasPrecision(20, 3);
            b.HasOne<LedgerAccountRow>().WithMany().HasForeignKey(x => x.AccountCode).OnDelete(DeleteBehavior.Restrict);
            b.HasIndex(x => new { x.JournalEntryId, x.AccountCode });
        });
    }
}

public sealed class LedgerAccountRow
{
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string Currency { get; set; } = "";
    public string AccountType { get; set; } = "";
    public bool IsActive { get; set; } = true;
}
public sealed class JournalEntryRow
{
    public Guid Id { get; set; }
    public string IdempotencyKey { get; set; } = "";
    public string Description { get; set; } = "";
    public DateTimeOffset PostedAt { get; set; }
    public Guid? ReversalOfEntryId { get; set; }
    public string PayloadHash { get; set; } = "";
    public List<JournalLineRow> Lines { get; set; } = [];
}
public sealed class JournalLineRow
{
    public Guid Id { get; set; }
    public Guid JournalEntryId { get; set; }
    public string AccountCode { get; set; } = "";
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public string Currency { get; set; } = "";
}
