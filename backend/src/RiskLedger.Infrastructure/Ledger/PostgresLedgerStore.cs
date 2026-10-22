using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using RiskLedger.Application.Ledger;
using RiskLedger.Domain;
using RiskLedger.Domain.Ledger;

namespace RiskLedger.Infrastructure.Ledger;

public sealed class PostgresLedgerStore(RiskLedgerDbContext db) : ILedgerStore
{
    public async Task<LedgerAccount> CreateAccountAsync(LedgerAccount account, CancellationToken ct)
    {
        db.Accounts.Add(new LedgerAccountRow { Code = account.Code, Name = account.Name, Currency = account.Currency, AccountType = account.AccountType, IsActive = true });
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateException ex) { db.ChangeTracker.Clear(); throw new InvalidOperationException($"Ledger account '{account.Code}' already exists or violates a database constraint.", ex); }
        return account;
    }

    public async Task<IReadOnlyCollection<LedgerAccount>> GetAccountsAsync(CancellationToken ct)
    {
        var rows = await db.Accounts.AsNoTracking().OrderBy(x => x.Code).ToListAsync(ct);
        return rows.Select(x => LedgerAccount.Create(x.Code, x.Name, x.Currency, x.AccountType)).ToArray();
    }

    public async Task<JournalEntry> PostAsync(JournalEntry entry, CancellationToken ct)
    {
        var key = NormalizeKey(entry.IdempotencyKey);
        var hash = Hash(entry);
        var existing = await LoadByKeyAsync(key, ct);
        if (existing is not null) return ExistingOrConflict(existing, hash);

        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var codes = entry.Lines.Select(x => x.AccountCode).Distinct().ToArray();
        var accounts = await db.Accounts.Where(x => codes.Contains(x.Code) && x.IsActive).ToDictionaryAsync(x => x.Code, ct);
        foreach (var line in entry.Lines)
        {
            if (!accounts.TryGetValue(line.AccountCode, out var account))
                throw new InvalidOperationException($"Ledger account '{line.AccountCode}' does not exist or is inactive.");
            if (account.Currency != line.Debit.Currency)
                throw new InvalidOperationException($"Journal currency does not match account '{line.AccountCode}'.");
        }
        if (entry.ReversalOfEntryId is Guid originalId &&
            await db.JournalEntries.AnyAsync(x => x.ReversalOfEntryId == originalId, ct))
            throw new InvalidOperationException("Journal entry has already been reversed.");

        db.JournalEntries.Add(FromDomain(entry, key, hash));
        try { await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); }
        catch (DbUpdateException ex)
        {
            await tx.RollbackAsync(ct); db.ChangeTracker.Clear();
            var winner = await LoadByKeyAsync(key, ct);
            if (winner is not null) return ExistingOrConflict(winner, hash);
            throw new InvalidOperationException("Journal posting conflicted with an existing reversal or database constraint.", ex);
        }
        return entry;
    }

    public async Task<JournalEntry> ReverseAsync(Guid entryId, string idempotencyKey, string reason, CancellationToken ct)
    {
        var original = await db.JournalEntries.AsNoTracking().Include(x => x.Lines).SingleOrDefaultAsync(x => x.Id == entryId, ct)
            ?? throw new KeyNotFoundException($"Journal entry '{entryId}' was not found.");
        if (await db.JournalEntries.AnyAsync(x => x.ReversalOfEntryId == entryId, ct))
            throw new InvalidOperationException("Journal entry has already been reversed.");
        return await PostAsync(ToDomain(original).Reverse(idempotencyKey, reason), ct);
    }

    public async Task<IReadOnlyCollection<JournalEntry>> GetEntriesAsync(CancellationToken ct) =>
        (await db.JournalEntries.AsNoTracking().Include(x => x.Lines).OrderByDescending(x => x.PostedAt).ThenByDescending(x => x.Id).ToListAsync(ct))
        .Select(ToDomain).ToArray();

    private Task<JournalEntryRow?> LoadByKeyAsync(string key, CancellationToken ct) =>
        db.JournalEntries.AsNoTracking().Include(x => x.Lines).SingleOrDefaultAsync(x => x.IdempotencyKey == key, ct);

    private static JournalEntry ExistingOrConflict(JournalEntryRow existing, string hash) =>
        existing.PayloadHash == hash ? ToDomain(existing) :
        throw new InvalidOperationException("Idempotency key was already used for a different journal payload.");

    private static string NormalizeKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key) || key.Trim().Length > 200)
            throw new ArgumentException("Idempotency key is required and must be at most 200 characters.", nameof(key));
        return key.Trim();
    }

    private static string Hash(JournalEntry e)
    {
        var canonical = $"{e.Description}|{e.ReversalOfEntryId}|{string.Join("|", e.Lines.OrderBy(x => x.AccountCode).Select(x => $"{x.AccountCode}:{x.Debit.Amount}:{x.Credit.Amount}:{x.Debit.Currency}"))}";
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
    }

    private static JournalEntryRow FromDomain(JournalEntry e, string key, string hash) => new()
    {
        Id = e.Id, IdempotencyKey = key, Description = e.Description, PostedAt = e.PostedAt,
        ReversalOfEntryId = e.ReversalOfEntryId, PayloadHash = hash,
        Lines = e.Lines.Select(x => new JournalLineRow { Id = Guid.NewGuid(), JournalEntryId = e.Id, AccountCode = x.AccountCode,
            Debit = x.Debit.Amount, Credit = x.Credit.Amount, Currency = x.Debit.Currency }).ToList()
    };

    private static JournalEntry ToDomain(JournalEntryRow e) => JournalEntry.Restore(
        e.Id, e.IdempotencyKey, e.Description, e.PostedAt, e.ReversalOfEntryId,
        e.Lines.Select(x => JournalLine.Create(x.AccountCode, new Money(x.Debit, x.Currency), new Money(x.Credit, x.Currency))));

}
