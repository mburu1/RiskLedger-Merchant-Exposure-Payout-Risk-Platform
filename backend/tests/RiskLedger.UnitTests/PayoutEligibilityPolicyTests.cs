using RiskLedger.Domain;
using RiskLedger.Domain.Payouts;
using RiskLedger.Domain.Risk;

namespace RiskLedger.UnitTests;

public sealed class PayoutEligibilityPolicyTests
{
    [Fact]
    public void Allows_payout_only_when_all_guardrails_pass()
    {
        var result = PayoutEligibilityPolicy.Evaluate(new(true, RiskOutcome.Approve,
            new Money(10000m, "KES"), new Money(2500m, "KES"), new Money(7000m, "KES"), false));

        Assert.True(result.IsEligible);
        Assert.Empty(result.ReasonCodes);
    }

    [Theory]
    [InlineData(false, RiskOutcome.Approve, false, "ORGANIZER_NOT_VERIFIED")]
    [InlineData(true, RiskOutcome.Review, false, "RISK_NOT_APPROVED")]
    [InlineData(true, RiskOutcome.Approve, true, "OPEN_DISPUTE_HOLD")]
    public void Denies_payout_when_a_guardrail_fails(bool verified, RiskOutcome outcome, bool dispute, string reason)
    {
        var result = PayoutEligibilityPolicy.Evaluate(new(verified, outcome,
            new Money(10000m, "KES"), new Money(1000m, "KES"), new Money(100m, "KES"), dispute));

        Assert.False(result.IsEligible);
        Assert.Contains(reason, result.ReasonCodes);
    }

    [Fact]
    public void Denies_payout_that_would_spend_reserved_funds()
    {
        var result = PayoutEligibilityPolicy.Evaluate(new(true, RiskOutcome.Approve,
            new Money(10000m, "KES"), new Money(3000m, "KES"), new Money(7001m, "KES"), false));

        Assert.False(result.IsEligible);
        Assert.Contains("INSUFFICIENT_UNRESERVED_FUNDS", result.ReasonCodes);
    }

    [Fact]
    public void Rejects_mixed_currency_inputs()
    {
        Assert.Throws<InvalidOperationException>(() => PayoutEligibilityPolicy.Evaluate(new(true, RiskOutcome.Approve,
            new Money(100m, "KES"), new Money(0m, "USD"), new Money(1m, "KES"), false)));
    }
}
