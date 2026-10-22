namespace RiskLedger.Domain.Ledger;

public sealed record JournalLine(string AccountCode, Money Debit, Money Credit)
{
    public static JournalLine Create(string accountCode, Money debit, Money credit)
    {
        if (string.IsNullOrWhiteSpace(accountCode))
            throw new ArgumentException("Account code is required.", nameof(accountCode));
        if (debit.Currency != credit.Currency)
            throw new ArgumentException("Debit and credit currency must match.");
        if (debit.Amount < 0 || credit.Amount < 0)
            throw new ArgumentOutOfRangeException(nameof(debit), "Debit and credit cannot be negative.");
        if ((debit.Amount == 0) == (credit.Amount == 0))
            throw new ArgumentException("Exactly one of debit or credit must be greater than zero.");
        return new JournalLine(accountCode.Trim(), debit, credit);
    }
}
