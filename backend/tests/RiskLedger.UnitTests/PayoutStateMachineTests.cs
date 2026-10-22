using RiskLedger.Domain.Payouts;

namespace RiskLedger.UnitTests;

public sealed class PayoutStateMachineTests
{
    [Fact]
    public void Provider_timeout_can_be_marked_unknown_without_blind_retry()
    {
        var transition = PayoutTransition.Apply(PayoutStatus.Submitted, PayoutStatus.Unknown, "PROVIDER_OUTCOME_UNKNOWN");
        Assert.Equal(PayoutStatus.Unknown, transition.To);
    }

    [Theory]
    [InlineData(PayoutStatus.Unknown, PayoutStatus.Succeeded)]
    [InlineData(PayoutStatus.Unknown, PayoutStatus.Declined)]
    [InlineData(PayoutStatus.Succeeded, PayoutStatus.Reconciled)]
    [InlineData(PayoutStatus.Declined, PayoutStatus.Reconciled)]
    public void Allows_authoritative_resolution_and_reconciliation(PayoutStatus from, PayoutStatus to)
    {
        Assert.Equal(to, PayoutTransition.Apply(from, to, "PROVIDER_CONFIRMED").To);
    }

    [Theory]
    [InlineData(PayoutStatus.Succeeded, PayoutStatus.Submitted)]
    [InlineData(PayoutStatus.Cancelled, PayoutStatus.Submitted)]
    [InlineData(PayoutStatus.Requested, PayoutStatus.Succeeded)]
    [InlineData(PayoutStatus.Reconciled, PayoutStatus.Succeeded)]
    public void Rejects_invalid_transitions(PayoutStatus from, PayoutStatus to)
    {
        Assert.Throws<InvalidOperationException>(() => PayoutTransition.Apply(from, to, "TEST"));
    }

    [Fact]
    public void Requires_reason_code_for_every_transition()
    {
        Assert.Throws<ArgumentException>(() => PayoutTransition.Apply(PayoutStatus.Requested, PayoutStatus.Reserved, " "));
    }
}
