using System.Collections.Concurrent;
using RiskLedger.Application.Ledger;
using RiskLedger.Domain.Ledger;

namespace RiskLedger.Infrastructure.Ledger;

/// <summary>Development-only adapter. Process-local and non-durable; replace with PostgreSQL before financial workflows.</summary>
public sealed class InMemoryLedgerStore : ILedgerStore
{
    private readonly ConcurrentDictionary<string, JournalEntry> _byKey = new(StringComparer.Ordinal);

    public Task<JournalEntry> PostAsync(JournalEntry entry, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var stored = _byKey.GetOrAdd(entry.IdempotencyKey, entry);
        if (stored.Description != entry.Description || !stored.Lines.SequenceEqual(entry.Lines))
            throw new InvalidOperationException("Idempotency key was already used for a different posting.");
        return Task.FromResult(stored);
    }

    public Task<IReadOnlyCollection<JournalEntry>> GetEntriesAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        IReadOnlyCollection<JournalEntry> entries = _byKey.Values.OrderByDescending(x => x.PostedAt).ToArray();
        return Task.FromResult(entries);
    }
}
