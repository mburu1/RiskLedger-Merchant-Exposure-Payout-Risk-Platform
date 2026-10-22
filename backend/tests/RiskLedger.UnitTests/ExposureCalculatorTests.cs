using RiskLedger.Domain;
using RiskLedger.Domain.Exposure;

namespace RiskLedger.UnitTests;

public sealed class ExposureCalculatorTests
{
    [Fact]
    public void Calculates_gross_covered_and_uncovered_exposure_from_eligible_inputs()
    {
        var result = ExposureCalculator.Calculate("org-1",
            DateTimeOffset.Parse("2026-10-22T10:00:00Z"),
            [
                new("ob-1", new Money(10000m, "KES"), true),
                new("ob-2", new Money(2500m, "KES"), true),
                new("ob-3", new Money(500m, "KES"), false)
            ],
            [
                new("funds-1", new Money(4000m, "KES"), true),
                new("funds-2", new Money(900m, "KES"), false)
            ]);

        Assert.Equal(ExposureCalculator.CalculationVersion, result.CalculationVersion);
        Assert.Equal(12500m, result.GrossEligibleObligations.Amount);
        Assert.Equal(4000m, result.EligibleFunds.Amount);
        Assert.Equal(8500m, result.UncoveredExposure.Amount);
        Assert.Equal(new[] { "ob-1", "ob-2" }, result.IncludedObligationIds);
        Assert.Equal(new[] { "funds-1" }, result.IncludedFundsIds);
    }

    [Fact]
    public void Uncovered_exposure_is_never_negative_when_eligible_funds_exceed_obligations()
    {
        var result = ExposureCalculator.Calculate("org-1", DateTimeOffset.UtcNow,
            [new("ob-1", new Money(100m, "KES"), true)],
            [new("funds-1", new Money(150m, "KES"), true)]);

        Assert.Equal(0m, result.UncoveredExposure.Amount);
    }

    [Fact]
    public void Rejects_mixed_currency_inputs_until_explicitly_normalized()
    {
        Assert.Throws<InvalidOperationException>(() => ExposureCalculator.Calculate("org-1",
            DateTimeOffset.UtcNow,
            [new("ob-1", new Money(100m, "KES"), true)],
            [new("funds-1", new Money(10m, "USD"), true)]));
    }

    [Fact]
    public void Rejects_duplicate_source_ids_to_prevent_double_counting()
    {
        Assert.Throws<InvalidOperationException>(() => ExposureCalculator.Calculate("org-1",
            DateTimeOffset.UtcNow,
            [new("ob-1", new Money(100m, "KES"), true), new("ob-1", new Money(100m, "KES"), true)],
            []));
    }

    [Fact]
    public void Rejects_negative_inputs()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ExposureCalculator.Calculate("org-1",
            DateTimeOffset.UtcNow,
            [new("ob-1", new Money(-1m, "KES"), true)],
            []));
    }
}
