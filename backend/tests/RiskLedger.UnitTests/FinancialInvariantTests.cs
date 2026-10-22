using RiskLedger.Domain;
using RiskLedger.Domain.Ledger;

namespace RiskLedger.UnitTests;

public sealed class FinancialInvariantTests
{
    [Fact]
    public void Money_normalizes_currency_and_preserves_exact_amount()
    {
        var money = new Money(10.12m, "kes");
        Assert.Equal(10.12m, money.Amount);
        Assert.Equal("KES", money.Currency);
        Assert.Equal(2, money.MinorUnits);
    }

    [Theory]
    [InlineData(10.126, "KES")]
    [InlineData(10.1, "JPY")]
    [InlineData(1.2345, "KWD")]
    public void Money_rejects_amounts_with_excess_currency_precision(decimal amount, string currency)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Money(amount, currency));
    }

    [Fact]
    public void Money_rejects_unsupported_currency()
    {
        Assert.Throws<NotSupportedException>(() => new Money(10m, "XYZ"));
    }

    [Fact]
    public void Money_rounds_calculated_values_using_midpoint_to_even()
    {
        Assert.Equal(1.24m, Money.Round(1.235m, "KES").Amount);
        Assert.Equal(1.22m, Money.Round(1.225m, "KES").Amount);
        Assert.Equal(2m, Money.Round(2.5m, "JPY").Amount);
    }

    [Fact]
    public void Money_rejects_cross_currency_arithmetic()
    {
        Assert.Throws<InvalidOperationException>(() => new Money(10m, "KES").Add(new Money(1m, "USD")));
    }

    [Fact]
    public void Journal_entry_accepts_balanced_posting()
    {
        var amount = new Money(1250m, "KES");
        var entry = JournalEntry.Post("booking-001", "Synthetic funds received", [
            JournalLine.Create("1000-CLEARING", amount, new Money(0m, "KES")),
            JournalLine.Create("2000-PAYABLE", new Money(0m, "KES"), amount)]);
        Assert.Equal(entry.Lines.Sum(x => x.Debit.Amount), entry.Lines.Sum(x => x.Credit.Amount));
    }

    [Fact]
    public void Journal_entry_rejects_unbalanced_posting()
    {
        Assert.Throws<InvalidOperationException>(() => JournalEntry.Post("bad-1", "Unbalanced", [
            JournalLine.Create("1000-CLEARING", new Money(100m, "KES"), new Money(0m, "KES")),
            JournalLine.Create("2000-PAYABLE", new Money(0m, "KES"), new Money(99m, "KES"))]));
    }

    [Fact]
    public void Journal_entry_balances_each_currency_independently()
    {
        var entry = JournalEntry.Post("multi-currency-1", "Balanced currencies", [
            Line("KES-CASH", 100m, 0m, "KES"),
            Line("KES-PAYABLE", 0m, 100m, "KES"),
            Line("USD-CASH", 10m, 0m, "USD"),
            Line("USD-PAYABLE", 0m, 10m, "USD")]);

        Assert.Equal(4, entry.Lines.Count);
    }

    [Fact]
    public void Journal_entry_rejects_currency_imbalance_even_if_another_currency_balances()
    {
        Assert.Throws<InvalidOperationException>(() => JournalEntry.Post("multi-currency-invalid", "USD imbalance", [
            Line("KES-CASH", 100m, 0m, "KES"),
            Line("KES-PAYABLE", 0m, 100m, "KES"),
            Line("USD-CASH", 10m, 0m, "USD"),
            Line("USD-PAYABLE", 0m, 9m, "USD")]));
    }

    [Fact]
    public void Journal_line_requires_exactly_one_nonzero_side()
    {
        Assert.Throws<ArgumentException>(() => JournalLine.Create("1000", new Money(10m, "KES"), new Money(10m, "KES")));
    }

    [Fact]
    public void Journal_entry_reverse_creates_compensating_entry_without_mutating_original()
    {
        var original = JournalEntry.Post("original", "Booking funds received", [
            Line("CLEARING", 100m, 0m, "KES"),
            Line("PAYABLE", 0m, 100m, "KES")]);
        var originalLines = original.Lines.ToArray();

        var reversal = original.Reverse("reversal-1", "Incorrect booking",
            DateTimeOffset.Parse("2026-10-22T09:00:00Z"));

        Assert.Equal(original.Id, reversal.ReversalOfEntryId);
        Assert.Equal(100m, reversal.Lines.Sum(x => x.Debit.Amount));
        Assert.Equal(100m, reversal.Lines.Sum(x => x.Credit.Amount));
        Assert.Equal(originalLines, original.Lines);
        Assert.Equal(0m, reversal.Lines[0].Debit.Amount);
        Assert.Equal(100m, reversal.Lines[0].Credit.Amount);
    }

    private static JournalLine Line(string account, decimal debit, decimal credit, string currency) =>
        JournalLine.Create(account, new Money(debit, currency), new Money(credit, currency));
}
