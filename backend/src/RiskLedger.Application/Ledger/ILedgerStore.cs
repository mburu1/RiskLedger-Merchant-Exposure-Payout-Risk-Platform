using RiskLedger.Domain.Ledger;

namespace RiskLedger.Application.Ledger;

public interface ILedgerStore
{
    Task<LedgerAccount> CreateAccountAsync(LedgerAccount account, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<LedgerAccount>> GetAccountsAsync(CancellationToken cancellationToken);
    Task<JournalEntry> PostAsync(JournalEntry entry, CancellationToken cancellationToken);
    Task<JournalEntry> ReverseAsync(Guid entryId, string idempotencyKey, string reason, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<JournalEntry>> GetEntriesAsync(CancellationToken cancellationToken);
}
