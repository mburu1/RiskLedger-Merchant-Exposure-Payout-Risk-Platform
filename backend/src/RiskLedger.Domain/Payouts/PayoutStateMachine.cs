namespace RiskLedger.Domain.Payouts;

public enum PayoutStatus { Requested, Reserved, Submitted, Succeeded, Declined, Unknown, Cancelled, Reconciled }

public sealed record PayoutTransition(PayoutStatus From, PayoutStatus To, string ReasonCode)
{
    public static PayoutTransition Apply(PayoutStatus current, PayoutStatus next, string reasonCode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reasonCode);
        if (!IsAllowed(current, next))
            throw new InvalidOperationException($"Payout transition {current} -> {next} is not permitted.");

        return new PayoutTransition(current, next, reasonCode);
    }

    private static bool IsAllowed(PayoutStatus from, PayoutStatus to) => (from, to) switch
    {
        (PayoutStatus.Requested, PayoutStatus.Reserved) => true,
        (PayoutStatus.Requested, PayoutStatus.Cancelled) => true,
        (PayoutStatus.Reserved, PayoutStatus.Submitted) => true,
        (PayoutStatus.Reserved, PayoutStatus.Cancelled) => true,
        (PayoutStatus.Submitted, PayoutStatus.Succeeded) => true,
        (PayoutStatus.Submitted, PayoutStatus.Declined) => true,
        (PayoutStatus.Submitted, PayoutStatus.Unknown) => true,
        (PayoutStatus.Unknown, PayoutStatus.Succeeded) => true,
        (PayoutStatus.Unknown, PayoutStatus.Declined) => true,
        (PayoutStatus.Succeeded, PayoutStatus.Reconciled) => true,
        (PayoutStatus.Declined, PayoutStatus.Reconciled) => true,
        _ => false
    };
}
