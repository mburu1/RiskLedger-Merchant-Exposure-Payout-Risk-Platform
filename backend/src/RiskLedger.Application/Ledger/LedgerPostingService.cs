using RiskLedger.Domain.Ledger;

namespace RiskLedger.Application.Ledger;

public sealed class LedgerPostingService(ILedgerStore store)
{
    public Task<LedgerAccount> CreateAccountAsync(string code, string name, string currency, string accountType, CancellationToken ct = default) =>
        store.CreateAccountAsync(LedgerAccount.Create(code, name, currency, accountType), ct);

    public Task<IReadOnlyCollection<LedgerAccount>> GetAccountsAsync(CancellationToken ct = default) => store.GetAccountsAsync(ct);

    public Task<JournalEntry> PostAsync(string idempotencyKey, string description, IEnumerable<JournalLine> lines, CancellationToken ct = default) =>
        store.PostAsync(JournalEntry.Post(idempotencyKey, description, lines), ct);

    public Task<JournalEntry> ReverseAsync(Guid entryId, string idempotencyKey, string reason, CancellationToken ct = default) =>
        store.ReverseAsync(entryId, idempotencyKey, reason, ct);

    public Task<IReadOnlyCollection<JournalEntry>> GetEntriesAsync(CancellationToken ct = default) => store.GetEntriesAsync(ct);
}
