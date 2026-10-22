using RiskLedger.Application.Ledger;
using RiskLedger.Domain;
using RiskLedger.Domain.Ledger;
using RiskLedger.Infrastructure.Ledger;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddProblemDetails();
builder.Services.AddHealthChecks();
builder.Services.AddSingleton<ILedgerStore, InMemoryLedgerStore>();
builder.Services.AddScoped<LedgerPostingService>();
builder.Services.AddOpenApi();

var app = builder.Build();
app.UseExceptionHandler();
app.MapHealthChecks("/health/live");
app.MapHealthChecks("/health/ready");
if (app.Environment.IsDevelopment()) app.MapOpenApi();

app.MapGet("/", () => Results.Ok(new { name = "RiskLedger API", mode = "educational-simulation", persistence = "in-memory-development-adapter", openApi = "/openapi/v1.json" }));
app.MapGet("/api/v1/ledger/entries", async (LedgerPostingService ledger, CancellationToken ct) =>
{
    var entries = await ledger.GetEntriesAsync(ct);
    return Results.Ok(entries.Select(ToResponse));
});
app.MapPost("/api/v1/ledger/demo-posting", async (DemoPostingRequest request, LedgerPostingService ledger, CancellationToken ct) =>
{
    if (string.IsNullOrWhiteSpace(request.IdempotencyKey))
        return Results.ValidationProblem(new Dictionary<string, string[]> { ["idempotencyKey"] = ["An idempotency key is required."] });
    if (string.IsNullOrWhiteSpace(request.Currency) || request.Currency.Length != 3 || !request.Currency.All(char.IsAsciiLetter))
        return Results.ValidationProblem(new Dictionary<string, string[]> { ["currency"] = ["Currency must be a three-letter code."] });
    if (!CurrencyCatalog.IsSupported(request.Currency))
        return Results.ValidationProblem(new Dictionary<string, string[]> { ["currency"] = [$"Currency '{request.Currency.ToUpperInvariant()}' is not supported."] });
    if (request.Amount <= 0)
        return Results.ValidationProblem(new Dictionary<string, string[]> { ["amount"] = ["Amount must be greater than zero."] });

    var minorUnits = CurrencyCatalog.GetMinorUnits(request.Currency);
    if (decimal.Round(request.Amount, minorUnits, MidpointRounding.ToEven) != request.Amount)
        return Results.ValidationProblem(new Dictionary<string, string[]> { ["amount"] = [$"Amount has more than {minorUnits} minor-unit digits for {request.Currency.ToUpperInvariant()}."] });

    var amount = new Money(request.Amount, request.Currency);
    var lines = new[]
    {
        JournalLine.Create("1000-PROCESSOR-CLEARING", amount, new Money(0m, amount.Currency)),
        JournalLine.Create("2000-MERCHANT-FUNDS-PAYABLE", new Money(0m, amount.Currency), amount)
    };
    var entry = await ledger.PostAsync(request.IdempotencyKey, "Synthetic booking funds received", lines, ct);
    return Results.Ok(ToResponse(entry));
});
app.Run();

static object ToResponse(JournalEntry entry) => new
{
    entry.Id, entry.IdempotencyKey, entry.Description, entry.PostedAt, entry.ReversalOfEntryId,
    currency = entry.Lines[0].Debit.Currency,
    debitTotal = entry.Lines.Sum(x => x.Debit.Amount),
    creditTotal = entry.Lines.Sum(x => x.Credit.Amount),
    lines = entry.Lines.Select(x => new { x.AccountCode, debit = x.Debit.Amount, credit = x.Credit.Amount, currency = x.Debit.Currency })
};

public sealed record DemoPostingRequest(string IdempotencyKey, decimal Amount, string Currency);
