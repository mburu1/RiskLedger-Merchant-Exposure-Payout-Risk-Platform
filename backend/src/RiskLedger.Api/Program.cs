using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
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
var connectionString = builder.Configuration["Database:ConnectionString"]
    ?? "Host=localhost;Port=5432;Database=riskledger;Username=riskledger;Password=local-only-change-me;Timeout=3;Command Timeout=5";
builder.Services.AddOptions<DatabaseOptions>().Configure(o => o.ConnectionString = connectionString)
    .Validate(o => !string.IsNullOrWhiteSpace(o.ConnectionString), "Database:ConnectionString is required.")
    .ValidateOnStart();
builder.Services.AddDbContext<RiskLedgerDbContext>(o => o.UseNpgsql(connectionString));
builder.Services.AddHealthChecks().AddCheck<DatabaseReadinessCheck>("postgres", tags: ["ready"]);
builder.Services.AddScoped<ILedgerStore, PostgresLedgerStore>();
builder.Services.AddScoped<LedgerPostingService>();
var app = builder.Build();
app.UseExceptionHandler();
app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions { Predicate = _ => false, ResponseWriter = HealthResponseWriter.WriteAsync });
app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions { Predicate = c => c.Tags.Contains("ready"), ResponseWriter = HealthResponseWriter.WriteAsync });
if (app.Environment.IsDevelopment()) { app.MapOpenApi(); app.MapScalarApiReference(o => o.WithTitle("RiskLedger API").WithTheme(ScalarTheme.Saturn)); }

app.MapGet("/", () => Results.Ok(new { name = "RiskLedger API", mode = "educational-simulation", persistence = "postgresql", openApi = "/openapi/v1.json", scalar = "/scalar/v1" })).WithTags("Service");
app.MapGet("/api/v1/ledger/accounts", async (LedgerPostingService ledger, CancellationToken ct) =>
    Results.Ok((await ledger.GetAccountsAsync(ct)).Select(a => new { a.Code, a.Name, a.Currency, a.AccountType, a.IsActive }))).WithTags("Ledger");
app.MapPost("/api/v1/ledger/accounts", async (CreateAccountRequest request, LedgerPostingService ledger, CancellationToken ct) =>
{
    var account = await ledger.CreateAccountAsync(request.Code, request.Name, request.Currency, request.AccountType, ct);
    return Results.Created($"/api/v1/ledger/accounts/{Uri.EscapeDataString(account.Code)}", new { account.Code, account.Name, account.Currency, account.AccountType, account.IsActive });
}).WithTags("Ledger");
app.MapGet("/api/v1/ledger/entries", async (LedgerPostingService ledger, CancellationToken ct) =>
    Results.Ok((await ledger.GetEntriesAsync(ct)).Select(ToResponse))).WithTags("Ledger");
app.MapPost("/api/v1/ledger/entries", async (PostJournalRequest request, LedgerPostingService ledger, CancellationToken ct) =>
{
    if (string.IsNullOrWhiteSpace(request.IdempotencyKey))
        return Results.ValidationProblem(new Dictionary<string, string[]> { ["idempotencyKey"] = ["An idempotency key is required."] });
    var lines = request.Lines.Select(x => JournalLine.Create(x.AccountCode, new Money(x.Debit, request.Currency), new Money(x.Credit, request.Currency))).ToArray();
    var entry = await ledger.PostAsync(request.IdempotencyKey, request.Description, lines, ct);
    return Results.Ok(ToResponse(entry));
}).WithTags("Ledger");
app.MapPost("/api/v1/ledger/entries/{id:guid}/reversals", async (Guid id, ReverseJournalRequest request, LedgerPostingService ledger, CancellationToken ct) =>
    Results.Ok(ToResponse(await ledger.ReverseAsync(id, request.IdempotencyKey, request.Reason, ct)))).WithTags("Ledger");
app.MapPost("/api/v1/ledger/demo-posting", async (DemoPostingRequest request, LedgerPostingService ledger, CancellationToken ct) =>
{
    var amount = new Money(request.Amount, request.Currency);
    var zero = new Money(0m, amount.Currency);
    var lines = new[] { JournalLine.Create("1000-PROCESSOR-CLEARING", amount, zero), JournalLine.Create("2000-MERCHANT-FUNDS-PAYABLE", zero, amount) };
    return Results.Ok(ToResponse(await ledger.PostAsync(request.IdempotencyKey, "Synthetic booking funds received", lines, ct)));
}).WithTags("Ledger");
app.Run();

static object ToResponse(JournalEntry e) => new
{
    e.Id, e.IdempotencyKey, e.Description, e.PostedAt, e.ReversalOfEntryId,
    currency = e.Lines[0].Debit.Currency,
    debitTotal = e.Lines.Sum(x => x.Debit.Amount), creditTotal = e.Lines.Sum(x => x.Credit.Amount),
    lines = e.Lines.Select(x => new { x.AccountCode, debit = x.Debit.Amount, credit = x.Credit.Amount, currency = x.Debit.Currency })
};
public sealed record CreateAccountRequest(string Code, string Name, string Currency, string AccountType);
public sealed record JournalLineRequest(string AccountCode, decimal Debit, decimal Credit);
public sealed record PostJournalRequest(string IdempotencyKey, string Description, string Currency, JournalLineRequest[] Lines);
public sealed record ReverseJournalRequest(string IdempotencyKey, string Reason);
public sealed record DemoPostingRequest(string IdempotencyKey, decimal Amount, string Currency);
public sealed class DatabaseOptions { public string ConnectionString { get; set; } = string.Empty; }
public sealed class DatabaseReadinessCheck(RiskLedgerDbContext db) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct = default)
    {
        try { return await db.Database.CanConnectAsync(ct) ? HealthCheckResult.Healthy("PostgreSQL accepts connections.") : HealthCheckResult.Unhealthy("PostgreSQL is not accepting connections."); }
        catch (Exception) { return HealthCheckResult.Unhealthy("PostgreSQL readiness check failed."); }
    }
}
public sealed class ProblemDetailsExceptionHandler(ILogger<ProblemDetailsExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken ct)
    {
        logger.LogError(exception, "Unhandled request failure. TraceId: {TraceId}", context.TraceIdentifier);
        var status = exception switch { KeyNotFoundException => 404, ArgumentException => 400, InvalidOperationException => 409, _ => 500 };
        context.Response.StatusCode = status;
        await context.Response.WriteAsJsonAsync(new ProblemDetails { Status = status, Title = status == 500 ? "An unexpected error occurred." : exception.Message, Detail = status == 500 ? "The request could not be completed. Use the trace identifier when contacting support." : exception.Message, Extensions = { ["traceId"] = context.TraceIdentifier } }, ct);
        return true;
    }
}
public static class HealthResponseWriter
{
    public static Task WriteAsync(HttpContext context, Microsoft.Extensions.Diagnostics.HealthChecks.HealthReport report)
    {
        context.Response.ContentType = "application/json";
        return context.Response.WriteAsJsonAsync(new { status = report.Status.ToString(), entries = report.Entries.ToDictionary(x => x.Key, x => new { status = x.Value.Status.ToString(), description = x.Value.Description }) });
    }
}
