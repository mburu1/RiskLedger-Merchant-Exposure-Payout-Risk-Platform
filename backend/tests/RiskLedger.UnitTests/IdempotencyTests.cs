using RiskLedger.Application.Ledger;
using RiskLedger.Domain;
using RiskLedger.Domain.Ledger;
using RiskLedger.Infrastructure.Ledger;

namespace RiskLedger.UnitTests;

public sealed class IdempotencyTests
{
    [Fact]
    public async Task Repeated_key_returns_existing_posting()
    {
        var service = new LedgerPostingService(new InMemoryLedgerStore());
        var lines = Lines(50);
        var first = await service.PostAsync("same-key", "Demo posting", lines);
        var second = await service.PostAsync("same-key", "Demo posting", lines);
        Assert.Equal(first.Id, second.Id);
        Assert.Single(await service.GetEntriesAsync());
    }

    [Fact]
    public async Task Reusing_key_with_different_command_fails()
    {
        var service = new LedgerPostingService(new InMemoryLedgerStore());
        await service.PostAsync("same-key", "First", Lines(50));
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.PostAsync("same-key", "Different", Lines(75)));
    }

    private static JournalLine[] Lines(decimal amount) => [
        JournalLine.Create("1000-CLEARING", new Money(amount, "KES"), new Money(0, "KES")),
        JournalLine.Create("2000-PAYABLE", new Money(0, "KES"), new Money(amount, "KES"))];
}
