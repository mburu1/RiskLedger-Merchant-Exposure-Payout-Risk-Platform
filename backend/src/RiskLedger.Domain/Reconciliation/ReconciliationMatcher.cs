namespace RiskLedger.Domain.Reconciliation;

public sealed record ProviderStatementLine(
    string ExternalReference,
    decimal Amount,
    string Currency,
    DateTimeOffset OccurredAt);

public sealed record InternalTransferRecord(
    string InternalReference,
    string ExternalReference,
    decimal Amount,
    string Currency,
    DateTimeOffset OccurredAt);

public enum ReconciliationStatus { Matched, MissingInternally, MissingExternally, AmountMismatch, CurrencyMismatch, DuplicateExternalReference }

public sealed record ReconciliationFinding(
    string ExternalReference,
    ReconciliationStatus Status,
    string? InternalReference,
    decimal? ExpectedAmount,
    decimal? ObservedAmount,
    string? ExpectedCurrency,
    string? ObservedCurrency);

/// <summary>
/// Pure reconciliation comparison. Findings are evidence only: this component never mutates
/// ledger entries or treats a mismatch as permission to rewrite financial history.
/// </summary>
public static class ReconciliationMatcher
{
    public static IReadOnlyList<ReconciliationFinding> Compare(
        IEnumerable<ProviderStatementLine> statement,
        IEnumerable<InternalTransferRecord> internalRecords)
    {
        ArgumentNullException.ThrowIfNull(statement);
        ArgumentNullException.ThrowIfNull(internalRecords);
        var external = statement.ToArray();
        var internalRows = internalRecords.ToArray();

        if (external.Any(x => string.IsNullOrWhiteSpace(x.ExternalReference)))
            throw new ArgumentException("Every provider statement line requires an external reference.", nameof(statement));
        if (internalRows.Any(x => string.IsNullOrWhiteSpace(x.ExternalReference)))
            throw new ArgumentException("Every internal transfer requires an external reference.", nameof(internalRecords));

        var findings = new List<ReconciliationFinding>();
        var duplicateRefs = external.GroupBy(x => x.ExternalReference, StringComparer.Ordinal)
            .Where(g => g.Count() > 1).Select(g => g.Key).ToHashSet(StringComparer.Ordinal);

        foreach (var reference in duplicateRefs.Order(StringComparer.Ordinal))
        {
            findings.Add(new(reference, ReconciliationStatus.DuplicateExternalReference, null, null, null, null, null));
        }

        foreach (var providerLine in external.Where(x => !duplicateRefs.Contains(x.ExternalReference)))
        {
            var matches = internalRows.Where(x => x.ExternalReference == providerLine.ExternalReference).ToArray();
            if (matches.Length == 0)
            {
                findings.Add(new(providerLine.ExternalReference, ReconciliationStatus.MissingInternally, null,
                    null, providerLine.Amount, null, providerLine.Currency));
                continue;
            }

            if (matches.Length > 1)
            {
                findings.Add(new(providerLine.ExternalReference, ReconciliationStatus.DuplicateExternalReference,
                    null, null, providerLine.Amount, null, providerLine.Currency));
                continue;
            }

            var internalLine = matches[0];
            if (!string.Equals(internalLine.Currency, providerLine.Currency, StringComparison.OrdinalIgnoreCase))
                findings.Add(new(providerLine.ExternalReference, ReconciliationStatus.CurrencyMismatch,
                    internalLine.InternalReference, internalLine.Amount, providerLine.Amount, internalLine.Currency, providerLine.Currency));
            else if (internalLine.Amount != providerLine.Amount)
                findings.Add(new(providerLine.ExternalReference, ReconciliationStatus.AmountMismatch,
                    internalLine.InternalReference, internalLine.Amount, providerLine.Amount, internalLine.Currency, providerLine.Currency));
            else
                findings.Add(new(providerLine.ExternalReference, ReconciliationStatus.Matched,
                    internalLine.InternalReference, internalLine.Amount, providerLine.Amount, internalLine.Currency, providerLine.Currency));
        }

        var externalRefs = external.Select(x => x.ExternalReference).ToHashSet(StringComparer.Ordinal);
        foreach (var internalLine in internalRows.Where(x => !externalRefs.Contains(x.ExternalReference)))
        {
            findings.Add(new(internalLine.ExternalReference, ReconciliationStatus.MissingExternally,
                internalLine.InternalReference, internalLine.Amount, null, internalLine.Currency, null));
        }

        return findings.OrderBy(x => x.ExternalReference, StringComparer.Ordinal).ThenBy(x => x.Status).ToArray();
    }
}
