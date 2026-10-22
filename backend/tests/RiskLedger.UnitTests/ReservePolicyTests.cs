using RiskLedger.Domain;
using RiskLedger.Domain.Reserves;

namespace RiskLedger.UnitTests;

public sealed class ReservePolicyTests
{
    [Theory]
    [InlineData(10000, 0.2, 2000)]
    [InlineData(12345.67, 0.125, 1543.21)]
    [InlineData(100, 0, 0)]
    public void Calculates_required_reserve_with_currency_rounding(decimal exposure, decimal rate, decimal expected)
    {
        var result = ReservePolicy.CalculateRequiredReserve(new Money(exposure, "KES"), rate);
        Assert.Equal(expected, result.Amount);
    }

    [Fact]
    public void Release_is_limited_to_amount_above_required_reserve()
    {
        var result = ReservePolicy.CalculateReleasableAmount(new Money(5000m, "KES"), new Money(4200m, "KES"));
        Assert.Equal(800m, result.Amount);
    }

    [Fact]
    public void Cannot_release_when_current_reserve_is_below_required_reserve()
    {
        var result = ReservePolicy.CalculateReleasableAmount(new Money(4000m, "KES"), new Money(4200m, "KES"));
        Assert.Equal(0m, result.Amount);
    }

    [Fact]
    public void Rejects_reserve_rate_above_one()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ReservePolicy.CalculateRequiredReserve(new Money(100m, "KES"), 1.1m));
    }
}
