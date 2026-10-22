namespace RiskLedger.Domain.Ledger;

/// <summary>A chart-of-accounts record. Account codes are stable identifiers.</summary>
public sealed record LedgerAccount
{
    public string Code { get; }
    public string Name { get; }
    public string Currency { get; }
    public string AccountType { get; }
    public bool IsActive { get; }

    private LedgerAccount(string code, string name, string currency, string accountType, bool isActive)
    {
        Code = code; Name = name; Currency = currency; AccountType = accountType; IsActive = isActive;
    }

    public static LedgerAccount Create(string code, string name, string currency, string accountType)
    {
        if (string.IsNullOrWhiteSpace(code) || code.Length > 64)
            throw new ArgumentException("Account code is required and must be at most 64 characters.", nameof(code));
        if (string.IsNullOrWhiteSpace(name) || name.Length > 160)
            throw new ArgumentException("Account name is required and must be at most 160 characters.", nameof(name));
        _ = new Money(0m, currency);
        var normalizedType = accountType?.Trim().ToLowerInvariant();
        if (normalizedType is not ("asset" or "liability" or "equity" or "revenue" or "expense"))
            throw new ArgumentException("Account type must be asset, liability, equity, revenue, or expense.", nameof(accountType));
        return new LedgerAccount(code.Trim(), name.Trim(), currency.ToUpperInvariant(), normalizedType, true);
    }
}
