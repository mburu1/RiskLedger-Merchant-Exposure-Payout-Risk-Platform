using RiskLedger.Application.Ledger;
using RiskLedger.Domain.Ledger;

namespace RiskLedger.Infrastructure.Ledger;

/// <summary>Legacy test adapter. Production API uses PostgreSQL.</summary>
public sealed class InMemoryLedgerStore : ILedgerStore
{
    private readonly object _gate = new();
    private readonly Dictionary<string, LedgerAccount> _accounts = new(StringComparer.Ordinal);
    private readonly Dictionary<string, JournalEntry> _entries = new(StringComparer.Ordinal);
    public Task<LedgerAccount> CreateAccountAsync(LedgerAccount account, CancellationToken ct)
    {
        lock (_gate) { if (!_accounts.TryAdd(account.Code, account)) throw new InvalidOperationException($"Ledger account '{account.Code}' already exists."); }
        return Task.FromResult(account);
    }
    public Task<IReadOnlyCollection<LedgerAccount>> GetAccountsAsync(CancellationToken ct)
    { lock (_gate) return Task.FromResult<IReadOnlyCollection<LedgerAccount>>(_accounts.Values.ToArray()); }
    public Task<JournalEntry> PostAsync(JournalEntry entry, CancellationToken ct)
    {
        lock (_gate)
        {
            if (_entries.TryGetValue(entry.IdempotencyKey, out var existing))
            {
                if (existing.Description != entry.Description || !Equivalent(existing, entry)) throw new InvalidOperationException("Idempotency key was already used for a different journal payload.");
                return Task.FromResult(existing);
            }
            foreach (var line in entry.Lines)
            {
                if (!_accounts.TryGetValue(line.AccountCode, out var account) || !account.IsActive) throw new InvalidOperationException($"Ledger account '{line.AccountCode}' does not exist or is inactive.");
                if (account.Currency != line.Debit.Currency) throw new InvalidOperationException($"Journal currency does not match account '{line.AccountCode}'.");
            }
            _entries.Add(entry.IdempotencyKey, entry);
            return Task.FromResult(entry);
        }
    }
    public Task<JournalEntry> ReverseAsync(Guid id, string key, string reason, CancellationToken ct)
    {
        lock (_gate)
        {
            var original = _entries.Values.SingleOrDefault(x => x.Id == id) ?? throw new KeyNotFoundException($"Journal entry '{id}' was not found.");
            if (_entries.Values.Any(x => x.ReversalOfEntryId == id)) throw new InvalidOperationException("Journal entry has already been reversed.");
            var reversal = original.Reverse(key, reason);
            return PostAsync(reversal, ct);
        }
    }
    public Task<IReadOnlyCollection<JournalEntry>> GetEntriesAsync(CancellationToken ct)
    { lock (_gate) return Task.FromResult<IReadOnlyCollection<JournalEntry>>(_entries.Values.OrderByDescending(x => x.PostedAt).ToArray()); }
    private static bool Equivalent(JournalEntry a, JournalEntry b) => a.Lines.OrderBy(x => x.AccountCode).Select(x => (x.AccountCode,x.Debit.Amount,x.Credit.Amount,x.Debit.Currency)).SequenceEqual(b.Lines.OrderBy(x => x.AccountCode).Select(x => (x.AccountCode,x.Debit.Amount,x.Credit.Amount,x.Debit.Currency)));
}
