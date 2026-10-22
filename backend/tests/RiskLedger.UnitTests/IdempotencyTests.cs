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
        var service = await CreateServiceAsync();
        var first = await service.PostAsync("same-key", "Demo posting", Lines(50));
        var second = await service.PostAsync("same-key", "Demo posting", Lines(50));
        Assert.Equal(first.Id, second.Id);
        Assert.Single(await service.GetEntriesAsync());
    }

    [Fact]
    public async Task Reusing_key_with_different_command_fails()
    {
        var service = await CreateServiceAsync();
        await service.PostAsync("same-key", "First", Lines(50));
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.PostAsync("same-key", "Different", Lines(75)));
    }

    [Fact]
    public async Task Reversal_is_a_new_entry_and_original_can_only_be_reversed_once()
    {
        var service = await CreateServiceAsync();
        var original = await service.PostAsync("original-key", "Synthetic booking", Lines(50));
        var reversal = await service.ReverseAsync(original.Id, "reverse-key", "Incorrect booking");
        Assert.NotEqual(original.Id, reversal.Id);
        Assert.Equal(original.Id, reversal.ReversalOfEntryId);
        Assert.Equal(50m, reversal.Lines.Sum(x => x.Credit.Amount));
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ReverseAsync(original.Id, "reverse-key-2", "Duplicate reversal"));
        Assert.Equal(2, (await service.GetEntriesAsync()).Count);
    }

    [Fact]
    public async Task Posting_requires_existing_accounts()
    {
        var service = new LedgerPostingService(new InMemoryLedgerStore());
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.PostAsync("missing-accounts", "Demo posting", Lines(10)));
    }

    private static async Task<LedgerPostingService> CreateServiceAsync()
    {
        var service = new LedgerPostingService(new InMemoryLedgerStore());
        await service.CreateAccountAsync("1000-CLEARING", "Processor clearing", "KES", "asset");
        await service.CreateAccountAsync("2000-PAYABLE", "Merchant payable", "KES", "liability");
        return service;
    }

    private static JournalLine[] Lines(decimal amount) => [
        JournalLine.Create("1000-CLEARING", new Money(amount, "KES"), new Money(0, "KES")),
        JournalLine.Create("2000-PAYABLE", new Money(0, "KES"), new Money(amount, "KES"))];
}
