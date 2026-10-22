namespace RiskLedger.Domain.Ledger;

/// <summary>
/// Immutable posted journal entry. Corrections are new compensating entries linked to
/// the original; the original entry is never edited.
/// </summary>
public sealed class JournalEntry
{
    public Guid Id { get; }
    public string IdempotencyKey { get; }
    public string Description { get; }
    public DateTimeOffset PostedAt { get; }
    public Guid? ReversalOfEntryId { get; }
    public IReadOnlyList<JournalLine> Lines { get; }

    private JournalEntry(Guid id, string idempotencyKey, string description,
        DateTimeOffset postedAt, Guid? reversalOfEntryId, IReadOnlyList<JournalLine> lines)
    {
        Id = id;
        IdempotencyKey = idempotencyKey;
        Description = description;
        PostedAt = postedAt;
        ReversalOfEntryId = reversalOfEntryId;
        Lines = Array.AsReadOnly(lines.ToArray());
    }

    public static JournalEntry Post(string idempotencyKey, string description,
        IEnumerable<JournalLine> lines, DateTimeOffset? postedAt = null) =>
        Create(idempotencyKey, description, lines, postedAt, reversalOfEntryId: null);

    /// <summary>Creates a new, fully compensating entry without changing the original.</summary>
    public JournalEntry Reverse(string idempotencyKey, string reason, DateTimeOffset? postedAt = null)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("A reversal reason is required.", nameof(reason));
        if (string.IsNullOrWhiteSpace(idempotencyKey))
            throw new ArgumentException("An idempotency key is required.", nameof(idempotencyKey));

        var reversalLines = Lines.Select(line => JournalLine.Create(
            line.AccountCode, line.Credit, line.Debit));
        return Create(idempotencyKey, $"Reversal of {Id}: {reason}", reversalLines,
            postedAt, reversalOfEntryId: Id);
    }

    private static JournalEntry Create(string idempotencyKey, string description,
        IEnumerable<JournalLine> lines, DateTimeOffset? postedAt, Guid? reversalOfEntryId)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
            throw new ArgumentException("An idempotency key is required.", nameof(idempotencyKey));
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("A description is required.", nameof(description));

        var materialized = lines?.ToArray() ?? throw new ArgumentNullException(nameof(lines));
        if (materialized.Length < 2)
            throw new InvalidOperationException("A journal entry requires at least two lines.");

        // Double-entry balance is evaluated independently for every currency. This
        // permits explicit FX journals while preventing one currency masking another.
        var currencies = materialized
            .SelectMany(x => new[] { x.Debit.Currency, x.Credit.Currency })
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        foreach (var currency in currencies)
        {
            var currencyLines = materialized.Where(x =>
                x.Debit.Currency == currency && x.Credit.Currency == currency).ToArray();
            var debitTotal = currencyLines.Sum(x => x.Debit.Amount);
            var creditTotal = currencyLines.Sum(x => x.Credit.Amount);

            if (debitTotal <= 0 || debitTotal != creditTotal)
                throw new InvalidOperationException(
                    $"Journal entry debits must equal credits and be greater than zero for {currency}.");
        }

        return new JournalEntry(Guid.NewGuid(), idempotencyKey.Trim(), description.Trim(),
            postedAt ?? DateTimeOffset.UtcNow, reversalOfEntryId, materialized);
    }
}
