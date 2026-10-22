using RiskLedger.Domain;
using RiskLedger.Domain.Risk;

namespace RiskLedger.UnitTests;

public sealed class RiskPolicyEvaluatorTests
{
    private static readonly RiskPolicy Policy = new("default", 1, 5000m, 20000m);

    [Fact]
    public void Missing_verification_restricts_and_records_reason()
    {
        var result = RiskPolicyEvaluator.Evaluate(Policy,
            new("org-1", false, new Money(100m, "KES"), false, DateTimeOffset.UtcNow));

        Assert.Equal(RiskOutcome.Restrict, result.Outcome);
        Assert.Contains("ORGANIZER_NOT_VERIFIED", result.ReasonCodes);
        Assert.Equal(1, result.PolicyVersion);
    }

    [Fact]
    public void Exposure_threshold_produces_review_with_explainable_reason()
    {
        var result = RiskPolicyEvaluator.Evaluate(Policy,
            new("org-1", true, new Money(5000m, "KES"), false, DateTimeOffset.UtcNow));

        Assert.Equal(RiskOutcome.Review, result.Outcome);
        Assert.Contains("EXPOSURE_REQUIRES_REVIEW", result.ReasonCodes);
    }

    [Fact]
    public void High_severity_dispute_restricts_even_when_exposure_is_low()
    {
        var result = RiskPolicyEvaluator.Evaluate(Policy,
            new("org-1", true, new Money(100m, "KES"), true, DateTimeOffset.UtcNow));

        Assert.Equal(RiskOutcome.Restrict, result.Outcome);
        Assert.Contains("OPEN_HIGH_SEVERITY_DISPUTE", result.ReasonCodes);
    }

    [Fact]
    public void Invalid_threshold_order_is_rejected()
    {
        var invalid = new RiskPolicy("default", 1, 20000m, 5000m);
        Assert.Throws<ArgumentException>(() => RiskPolicyEvaluator.Evaluate(invalid,
            new("org-1", true, new Money(100m, "KES"), false, DateTimeOffset.UtcNow)));
    }
}
