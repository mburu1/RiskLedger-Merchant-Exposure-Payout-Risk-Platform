using RiskLedger.Domain;

namespace RiskLedger.Domain.Reserves;

public static class ReservePolicy
{
    public static Money CalculateRequiredReserve(Money uncoveredExposure, decimal reserveRate)
    {
        ArgumentNullException.ThrowIfNull(uncoveredExposure);
        if (uncoveredExposure.Amount < 0m)
            throw new ArgumentOutOfRangeException(nameof(uncoveredExposure));
        if (reserveRate is < 0m or > 1m)
            throw new ArgumentOutOfRangeException(nameof(reserveRate), "Reserve rate must be between 0 and 1 inclusive.");

        return Money.Round(uncoveredExposure.Amount * reserveRate, uncoveredExposure.Currency);
    }

    public static Money CalculateReleasableAmount(Money currentReserve, Money requiredReserve)
    {
        ArgumentNullException.ThrowIfNull(currentReserve);
        ArgumentNullException.ThrowIfNull(requiredReserve);
        if (currentReserve.Currency != requiredReserve.Currency)
            throw new InvalidOperationException("Reserve amounts must use the same currency.");
        if (currentReserve.Amount < 0m || requiredReserve.Amount < 0m)
            throw new ArgumentOutOfRangeException(nameof(currentReserve), "Reserve amounts cannot be negative.");

        return new Money(Math.Max(0m, currentReserve.Amount - requiredReserve.Amount), currentReserve.Currency);
    }
}
