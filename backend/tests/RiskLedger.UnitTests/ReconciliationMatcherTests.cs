using RiskLedger.Domain.Reconciliation;

namespace RiskLedger.UnitTests;

public sealed class ReconciliationMatcherTests
{
    [Fact]
    public void Identifies_matches_missing_records_amount_and_currency_mismatches()
    {
        var findings = ReconciliationMatcher.Compare(
        [
            new("ext-match", 100m, "KES", DateTimeOffset.UtcNow),
            new("ext-missing", 25m, "KES", DateTimeOffset.UtcNow),
            new("ext-amount", 50m, "KES", DateTimeOffset.UtcNow),
            new("ext-currency", 70m, "USD", DateTimeOffset.UtcNow)
        ],
        [
            new("int-1", "ext-match", 100m, "KES", DateTimeOffset.UtcNow),
            new("int-2", "ext-amount", 55m, "KES", DateTimeOffset.UtcNow),
            new("int-3", "ext-currency", 70m, "KES", DateTimeOffset.UtcNow),
            new("int-4", "int-only", 10m, "KES", DateTimeOffset.UtcNow)
        ]);

        Assert.Contains(findings, x => x.ExternalReference == "ext-match" && x.Status == ReconciliationStatus.Matched);
        Assert.Contains(findings, x => x.ExternalReference == "ext-missing" && x.Status == ReconciliationStatus.MissingInternally);
        Assert.Contains(findings, x => x.ExternalReference == "ext-amount" && x.Status == ReconciliationStatus.AmountMismatch);
        Assert.Contains(findings, x => x.ExternalReference == "ext-currency" && x.Status == ReconciliationStatus.CurrencyMismatch);
        Assert.Contains(findings, x => x.ExternalReference == "int-only" && x.Status == ReconciliationStatus.MissingExternally);
    }

    [Fact]
    public void Flags_duplicate_provider_references_instead_of_guessing_a_match()
    {
        var findings = ReconciliationMatcher.Compare(
        [
            new("ext-1", 10m, "KES", DateTimeOffset.UtcNow),
            new("ext-1", 10m, "KES", DateTimeOffset.UtcNow)
        ], []);

        Assert.Contains(findings, x => x.Status == ReconciliationStatus.DuplicateExternalReference);
        Assert.DoesNotContain(findings, x => x.Status == ReconciliationStatus.Matched);
    }
}
