using RiskLedger.Domain.Ledger;

namespace RiskLedger.Application.Ledger;

public interface ILedgerStore
{
    Task<JournalEntry> PostAsync(JournalEntry entry, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<JournalEntry>> GetEntriesAsync(CancellationToken cancellationToken);
}
