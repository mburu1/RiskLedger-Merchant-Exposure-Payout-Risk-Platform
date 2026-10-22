using RiskLedger.Domain;
using RiskLedger.Domain.Ledger;

namespace RiskLedger.UnitTests;

public sealed class FinancialInvariantTests
{
    [Fact]
    public void Money_normalizes_currency_and_rounds_to_two_decimals()
    {
        var money = new Money(10.126m, "kes");
        Assert.Equal(10.13m, money.Amount);
        Assert.Equal("KES", money.Currency);
    }

    [Fact]
    public void Money_rejects_cross_currency_arithmetic()
    {
        Assert.Throws<InvalidOperationException>(() => new Money(10, "KES").Add(new Money(1, "USD")));
    }

    [Fact]
    public void Journal_entry_accepts_balanced_posting()
    {
        var amount = new Money(1250m, "KES");
        var entry = JournalEntry.Post("booking-001", "Synthetic funds received", [
            JournalLine.Create("1000-CLEARING", amount, new Money(0, "KES")),
            JournalLine.Create("2000-PAYABLE", new Money(0, "KES"), amount)]);
        Assert.Equal(entry.Lines.Sum(x => x.Debit.Amount), entry.Lines.Sum(x => x.Credit.Amount));
    }

    [Fact]
    public void Journal_entry_rejects_unbalanced_posting()
    {
        Assert.Throws<InvalidOperationException>(() => JournalEntry.Post("bad-1", "Unbalanced", [
            JournalLine.Create("1000-CLEARING", new Money(100, "KES"), new Money(0, "KES")),
            JournalLine.Create("2000-PAYABLE", new Money(0, "KES"), new Money(99, "KES"))]));
    }

    [Fact]
    public void Journal_entry_rejects_mixed_currency()
    {
        Assert.Throws<InvalidOperationException>(() => JournalEntry.Post("mixed-1", "Mixed currencies", [
            JournalLine.Create("1000-CLEARING", new Money(100, "KES"), new Money(0, "KES")),
            JournalLine.Create("2000-PAYABLE", new Money(0, "USD"), new Money(100, "USD"))]));
    }

    [Fact]
    public void Journal_line_requires_exactly_one_nonzero_side()
    {
        Assert.Throws<ArgumentException>(() => JournalLine.Create("1000", new Money(10, "KES"), new Money(10, "KES")));
    }
}
