namespace RiskLedger.Domain;

/// <summary>
/// Currency metadata used by the domain. Unknown currencies are rejected rather than
/// silently assigned an assumed two-decimal scale.
/// </summary>
public static class CurrencyCatalog
{
    private static readonly IReadOnlyDictionary<string, int> MinorUnitsByCode =
        new Dictionary<string, int>(StringComparer.Ordinal)
        {
            ["KES"] = 2,
            ["USD"] = 2,
            ["EUR"] = 2,
            ["GBP"] = 2,
            ["JPY"] = 0,
            ["KWD"] = 3
        };

    public static bool IsSupported(string code) =>
        !string.IsNullOrWhiteSpace(code) &&
        MinorUnitsByCode.ContainsKey(code.ToUpperInvariant());

    public static int GetMinorUnits(string code)
    {
        ArgumentNullException.ThrowIfNull(code);
        var normalized = code.ToUpperInvariant();
        return MinorUnitsByCode.TryGetValue(normalized, out var minorUnits)
            ? minorUnits
            : throw new NotSupportedException($"Currency '{normalized}' is not supported.");
    }
}

/// <summary>
/// Exact decimal money. Inputs with excess minor-unit precision are rejected, not
/// silently rounded. Call Round explicitly for calculated amounts that require rounding.
/// </summary>
public sealed record Money
{
    public decimal Amount { get; }
    public string Currency { get; }
    public int MinorUnits => CurrencyCatalog.GetMinorUnits(Currency);

    public Money(decimal amount, string currency)
    {
        if (string.IsNullOrWhiteSpace(currency) || currency.Length != 3 || !currency.All(char.IsAsciiLetter))
            throw new ArgumentException("Currency must be a three-letter ISO-style code.", nameof(currency));

        Currency = currency.ToUpperInvariant();
        var minorUnits = CurrencyCatalog.GetMinorUnits(Currency);
        if (decimal.Round(amount, minorUnits, MidpointRounding.ToEven) != amount)
            throw new ArgumentOutOfRangeException(nameof(amount),
                $"Amount has more than {minorUnits} minor-unit digits for {Currency}.");

        Amount = amount;
    }

    /// <summary>Rounds a calculated amount using the documented midpoint-to-even policy.</summary>
    public static Money Round(decimal amount, string currency)
    {
        var normalized = currency?.ToUpperInvariant()
            ?? throw new ArgumentNullException(nameof(currency));
        var minorUnits = CurrencyCatalog.GetMinorUnits(normalized);
        return new Money(decimal.Round(amount, minorUnits, MidpointRounding.ToEven), normalized);
    }

    public Money Add(Money other)
    {
        EnsureSameCurrency(other);
        return new Money(Amount + other.Amount, Currency);
    }

    public Money Subtract(Money other)
    {
        EnsureSameCurrency(other);
        return new Money(Amount - other.Amount, Currency);
    }

    private void EnsureSameCurrency(Money other)
    {
        ArgumentNullException.ThrowIfNull(other);
        if (!string.Equals(Currency, other.Currency, StringComparison.Ordinal))
            throw new InvalidOperationException($"Currency mismatch: {Currency} and {other.Currency}.");
    }
}
