using Microsoft.EntityFrameworkCore;
using RiskLedger.Application.Ledger;
using RiskLedger.Domain;
using RiskLedger.Domain.Ledger;
using RiskLedger.Infrastructure;
using RiskLedger.Infrastructure.Ledger;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ProblemDetailsExceptionHandler>();
builder.Services.AddOpenApi();
builder.Services.AddOptions<DatabaseOptions>()
    .Bind(builder.Configuration.GetSection(DatabaseOptions.SectionName))
    .Validate(o => !string.IsNullOrWhiteSpace(o.ConnectionString), "Database:ConnectionString is required.")
    .ValidateOnStart();

var connectionString = builder.Configuration["Database:ConnectionString"]
    ?? "Host=localhost;Port=5432;Database=riskledger;Username=riskledger;Password=local-only-change-me";
builder.Services.AddDbContext<RiskLedgerDbContext>(options => options.UseNpgsql(connectionString));
builder.Services.AddHealthChecks().AddCheck<DatabaseReadinessCheck>("postgres", tags: ["ready"]);
builder.Services.AddSingleton<ILedgerStore, InMemoryLedgerStore>();
builder.Services.AddScoped<LedgerPostingService>();

var app = builder.Build();
app.UseExceptionHandler();
app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") });
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options => options.WithTitle("RiskLedger API").WithTheme(ScalarTheme.Saturn));
}

app.MapGet("/", () => Results.Ok(new { name = "RiskLedger API", mode = "educational-simulation", persistence = "postgresql-configured; ledger-demo-in-memory", openApi = "/openapi/v1.json", scalar = "/scalar/v1" })).WithTags("Service");
app.MapGet("/api/v1/ledger/entries", async (LedgerPostingService ledger, CancellationToken ct) =>
    Results.Ok((await ledger.GetEntriesAsync(ct)).Select(ToResponse))).WithTags("Ledger");
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
    var zero = new Money(0m, amount.Currency);
    var lines = new[] { JournalLine.Create("1000-PROCESSOR-CLEARING", amount, zero), JournalLine.Create("2000-MERCHANT-FUNDS-PAYABLE", zero, amount) };
    var entry = await ledger.PostAsync(request.IdempotencyKey, "Synthetic booking funds received", lines, ct);
    return Results.Ok(ToResponse(entry));
}).WithTags("Ledger");
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
