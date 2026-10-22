namespace RiskLedger.Domain.Ledger;

/// <summary>Immutable posted journal entry. Corrections are compensating entries.</summary>
public sealed class JournalEntry
{
    public Guid Id { get; }
    public string IdempotencyKey { get; }
    public string Description { get; }
    public DateTimeOffset PostedAt { get; }
    public Guid? ReversalOfEntryId { get; }
    public IReadOnlyList<JournalLine> Lines { get; }

    private JournalEntry(Guid id, string idempotencyKey, string description, DateTimeOffset postedAt, Guid? reversalOfEntryId, IReadOnlyList<JournalLine> lines)
    {
        Id = id; IdempotencyKey = idempotencyKey; Description = description; PostedAt = postedAt;
        ReversalOfEntryId = reversalOfEntryId; Lines = Array.AsReadOnly(lines.ToArray());
    }

    public static JournalEntry Post(string idempotencyKey, string description, IEnumerable<JournalLine> lines, DateTimeOffset? postedAt = null) =>
        Create(Guid.NewGuid(), idempotencyKey, description, lines, postedAt ?? DateTimeOffset.UtcNow, null);

    /// <summary>Hydrates a persisted immutable journal without changing its identity.</summary>
    public static JournalEntry Restore(Guid id, string idempotencyKey, string description, DateTimeOffset postedAt, Guid? reversalOfEntryId, IEnumerable<JournalLine> lines) =>
        Create(id, idempotencyKey, description, lines, postedAt, reversalOfEntryId);

    public JournalEntry Reverse(string idempotencyKey, string reason, DateTimeOffset? postedAt = null)
    {
        if (string.IsNullOrWhiteSpace(reason)) throw new ArgumentException("A reversal reason is required.", nameof(reason));
        if (string.IsNullOrWhiteSpace(idempotencyKey)) throw new ArgumentException("An idempotency key is required.", nameof(idempotencyKey));
        return Create(Guid.NewGuid(), idempotencyKey, $"Reversal of {Id}: {reason}",
            Lines.Select(x => JournalLine.Create(x.AccountCode, x.Credit, x.Debit)), postedAt ?? DateTimeOffset.UtcNow, Id);
    }

    private static JournalEntry Create(Guid id, string idempotencyKey, string description, IEnumerable<JournalLine> lines, DateTimeOffset postedAt, Guid? reversalOfEntryId)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey)) throw new ArgumentException("An idempotency key is required.", nameof(idempotencyKey));
        if (idempotencyKey.Trim().Length > 200) throw new ArgumentException("Idempotency key must be at most 200 characters.", nameof(idempotencyKey));
        if (string.IsNullOrWhiteSpace(description)) throw new ArgumentException("A description is required.", nameof(description));
        var materialized = lines?.ToArray() ?? throw new ArgumentNullException(nameof(lines));
        if (materialized.Length < 2) throw new InvalidOperationException("A journal entry requires at least two lines.");
        var currencies = materialized.Select(x => x.Debit.Currency).Distinct(StringComparer.Ordinal).ToArray();
        foreach (var currency in currencies)
        {
            var currencyLines = materialized.Where(x => x.Debit.Currency == currency).ToArray();
            var debits = currencyLines.Sum(x => x.Debit.Amount);
            var credits = currencyLines.Sum(x => x.Credit.Amount);
            if (debits <= 0 || debits != credits)
                throw new InvalidOperationException($"Journal entry debits must equal credits and be greater than zero for {currency}.");
        }
        return new JournalEntry(id, idempotencyKey.Trim(), description.Trim(), postedAt, reversalOfEntryId, materialized);
    }
}
