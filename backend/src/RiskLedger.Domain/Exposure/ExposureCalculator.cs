using RiskLedger.Domain;

namespace RiskLedger.Domain.Exposure;

/// <summary>
/// Versioned deterministic calculation over a caller-supplied point-in-time snapshot.
/// This component does not fetch data or determine eligibility.
/// </summary>
public static class ExposureCalculator
{
    public const string CalculationVersion = "exposure-v1";

    public static ExposureSnapshot Calculate(
        string organizerId,
        DateTimeOffset calculatedAt,
        IEnumerable<SupplierObligationInput> obligations,
        IEnumerable<EligibleFundsInput> eligibleFunds)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(organizerId);
        ArgumentNullException.ThrowIfNull(obligations);
        ArgumentNullException.ThrowIfNull(eligibleFunds);

        var obligationRows = obligations.ToArray();
        var fundsRows = eligibleFunds.ToArray();
        if (obligationRows.Length + fundsRows.Length == 0)
            throw new ArgumentException("At least one source input is required to establish the calculation currency.");

        if (obligationRows.Any(x => x.Amount is null) || fundsRows.Any(x => x.Amount is null))
            throw new ArgumentException("Exposure inputs must include an amount.");

        EnsureUniqueIds(obligationRows.Select(x => x.Id), "supplier obligation");
        EnsureUniqueIds(fundsRows.Select(x => x.Id), "eligible funds");

        var allCurrencies = obligationRows.Select(x => x.Amount.Currency)
            .Concat(fundsRows.Select(x => x.Amount.Currency))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        if (allCurrencies.Length != 1)
            throw new InvalidOperationException("Exposure inputs must be normalized to one currency before calculation.");

        var currency = allCurrencies[0];
        if (obligationRows.Any(x => x.Amount.Amount < 0m) || fundsRows.Any(x => x.Amount.Amount < 0m))
            throw new ArgumentOutOfRangeException(nameof(obligations), "Exposure inputs cannot contain negative amounts.");

        var gross = obligationRows.Where(x => x.IsEligible).Sum(x => x.Amount.Amount);
        var covered = fundsRows.Where(x => x.IsEligible).Sum(x => x.Amount.Amount);
        var uncovered = Math.Max(0m, gross - covered);

        return new ExposureSnapshot(
            organizerId,
            CalculationVersion,
            calculatedAt,
            currency,
            Money.Round(gross, currency),
            Money.Round(covered, currency),
            Money.Round(uncovered, currency),
            obligationRows.Where(x => x.IsEligible).Select(x => x.Id).Order(StringComparer.Ordinal).ToArray(),
            fundsRows.Where(x => x.IsEligible).Select(x => x.Id).Order(StringComparer.Ordinal).ToArray());
    }

    private static void EnsureUniqueIds(IEnumerable<string> ids, string label)
    {
        var values = ids.ToArray();
        if (values.Any(string.IsNullOrWhiteSpace))
            throw new ArgumentException($"Every {label} must have a non-empty identifier.");
        if (values.Distinct(StringComparer.Ordinal).Count() != values.Length)
            throw new InvalidOperationException($"Duplicate {label} identifiers are not permitted in an exposure snapshot.");
    }
}

public sealed record SupplierObligationInput(string Id, Money Amount, bool IsEligible);
public sealed record EligibleFundsInput(string Id, Money Amount, bool IsEligible);

public sealed record ExposureSnapshot(
    string OrganizerId,
    string CalculationVersion,
    DateTimeOffset CalculatedAt,
    string Currency,
    Money GrossEligibleObligations,
    Money EligibleFunds,
    Money UncoveredExposure,
    IReadOnlyList<string> IncludedObligationIds,
    IReadOnlyList<string> IncludedFundsIds);
