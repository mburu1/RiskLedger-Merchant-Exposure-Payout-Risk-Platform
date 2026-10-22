using RiskLedger.Domain.Ledger;

namespace RiskLedger.Application.Ledger;

public sealed class LedgerPostingService(ILedgerStore store)
{
    public Task<JournalEntry> PostAsync(string idempotencyKey, string description, IEnumerable<JournalLine> lines, CancellationToken cancellationToken = default)
    {
        var entry = JournalEntry.Post(idempotencyKey, description, lines);
        return store.PostAsync(entry, cancellationToken);
    }

    public Task<IReadOnlyCollection<JournalEntry>> GetEntriesAsync(CancellationToken cancellationToken = default) => store.GetEntriesAsync(cancellationToken);
}
