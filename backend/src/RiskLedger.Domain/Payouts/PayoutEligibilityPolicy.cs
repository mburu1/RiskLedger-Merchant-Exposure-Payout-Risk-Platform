using RiskLedger.Domain;
using RiskLedger.Domain.Risk;

namespace RiskLedger.Domain.Payouts;

public sealed record PayoutEligibilityInput(
    bool IsVerified,
    RiskOutcome LatestRiskOutcome,
    Money AvailableFunds,
    Money ActiveReserves,
    Money RequestedAmount,
    bool HasOpenDisputeHold);

public sealed record PayoutEligibilityResult(bool IsEligible, IReadOnlyList<string> ReasonCodes);

/// <summary>
/// Fail-closed payout eligibility policy. This is a decision helper, not a funds reservation
/// or provider call; callers must perform the final check and reservation transactionally.
/// </summary>
public static class PayoutEligibilityPolicy
{
    public static PayoutEligibilityResult Evaluate(PayoutEligibilityInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        EnsureSameCurrency(input.AvailableFunds, input.ActiveReserves, input.RequestedAmount);

        if (input.AvailableFunds.Amount < 0m || input.ActiveReserves.Amount < 0m)
            throw new ArgumentOutOfRangeException(nameof(input), "Available funds and reserves cannot be negative.");
        if (input.RequestedAmount.Amount <= 0m)
            throw new ArgumentOutOfRangeException(nameof(input), "Payout amount must be greater than zero.");

        var reasons = new List<string>();
        if (!input.IsVerified) reasons.Add("ORGANIZER_NOT_VERIFIED");
        if (input.LatestRiskOutcome != RiskOutcome.Approve) reasons.Add("RISK_NOT_APPROVED");
        if (input.HasOpenDisputeHold) reasons.Add("OPEN_DISPUTE_HOLD");

        var releasable = input.AvailableFunds.Amount - input.ActiveReserves.Amount;
        if (releasable < input.RequestedAmount.Amount) reasons.Add("INSUFFICIENT_UNRESERVED_FUNDS");

        return new PayoutEligibilityResult(reasons.Count == 0, reasons.AsReadOnly());
    }

    private static void EnsureSameCurrency(params Money[] amounts)
    {
        var currencies = amounts.Select(x => x.Currency).Distinct(StringComparer.Ordinal).ToArray();
        if (currencies.Length != 1)
            throw new InvalidOperationException("Payout eligibility inputs must use the same currency.");
    }
}
