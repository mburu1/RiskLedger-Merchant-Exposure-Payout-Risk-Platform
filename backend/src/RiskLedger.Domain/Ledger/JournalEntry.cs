namespace RiskLedger.Domain.Ledger;

public sealed class JournalEntry
{
    public Guid Id { get; }
    public string IdempotencyKey { get; }
    public string Description { get; }
    public DateTimeOffset PostedAt { get; }
    public IReadOnlyList<JournalLine> Lines { get; }

    private JournalEntry(Guid id, string idempotencyKey, string description, DateTimeOffset postedAt, IReadOnlyList<JournalLine> lines)
    {
        Id = id;
        IdempotencyKey = idempotencyKey;
        Description = description;
        PostedAt = postedAt;
        Lines = lines;
    }

    public static JournalEntry Post(string idempotencyKey, string description, IEnumerable<JournalLine> lines, DateTimeOffset? postedAt = null)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey)) throw new ArgumentException("An idempotency key is required.", nameof(idempotencyKey));
        if (string.IsNullOrWhiteSpace(description)) throw new ArgumentException("A description is required.", nameof(description));
        var materialized = lines?.ToArray() ?? throw new ArgumentNullException(nameof(lines));
        if (materialized.Length < 2) throw new InvalidOperationException("A journal entry requires at least two lines.");
        var currencies = materialized.SelectMany(x => new[] { x.Debit.Currency, x.Credit.Currency }).Distinct(StringComparer.Ordinal).ToArray();
        if (currencies.Length != 1) throw new InvalidOperationException("A journal entry must balance in a single currency.");
        var debitTotal = materialized.Sum(x => x.Debit.Amount);
        var creditTotal = materialized.Sum(x => x.Credit.Amount);
        if (debitTotal <= 0 || debitTotal != creditTotal) throw new InvalidOperationException("Journal entry debits must equal credits and be greater than zero.");
        return new JournalEntry(Guid.NewGuid(), idempotencyKey.Trim(), description.Trim(), postedAt ?? DateTimeOffset.UtcNow, Array.AsReadOnly(materialized));
    }
}
