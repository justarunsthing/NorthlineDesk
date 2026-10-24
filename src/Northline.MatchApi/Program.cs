using System.Text.Json;
using Northline.Data;
using Northline.Match;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
});

var connectionString = builder.Configuration.GetConnectionString("Northline");
var shadowKey = builder.Configuration["ShadowKey"] ?? "";
var vaultUnresolved = VaultUnresolved(connectionString);
var database = string.IsNullOrWhiteSpace(connectionString) || vaultUnresolved
    ? null
    : new EstateDatabase(connectionString);

var app = builder.Build();

app.MapGet("/", () => Results.Text(
    "Northline match service. The desk is the Web Forms app. POST /match with {\"invoiceNumber\":\"INV-1001\"}.",
    "text/plain"));

app.MapGet("/health", async (CancellationToken cancellationToken) =>
{
    if (vaultUnresolved)
        return Results.Json(new { status = "failed", detail = "The SQL connection has not resolved from Key Vault." }, statusCode: StatusCodes.Status503ServiceUnavailable);

    if (database is null)
        return Results.Ok(new { status = "ok", sql = "not-configured", mode = "in-memory-estate" });

    try
    {
        _ = await database.MatchFromServiceAsync(CalloutEstate.InvoiceNumber, cancellationToken);
        return Results.Ok(new { status = "ok", sql = "connected" });
    }
    catch (Exception exception)
    {
        return Results.Json(new { status = "failed", detail = exception.Message }, statusCode: StatusCodes.Status503ServiceUnavailable);
    }
});

app.MapGet("/match/{invoiceNumber}", (string invoiceNumber, CancellationToken cancellationToken) =>
    Match(invoiceNumber, cancellationToken));

app.MapPost("/match", (MatchRequest request, CancellationToken cancellationToken) =>
    Match(request.InvoiceNumber, cancellationToken));

app.MapPost("/shadow/{invoiceNumber}", async (string invoiceNumber, HttpRequest request, CancellationToken cancellationToken) =>
{
    if (VaultUnresolved(shadowKey))
        return Results.Json(new { detail = "The shadow key has not resolved from Key Vault." }, statusCode: StatusCodes.Status503ServiceUnavailable);

    if (string.IsNullOrEmpty(shadowKey) || request.Headers["X-Shadow-Key"] != shadowKey)
        return Results.Json(new { detail = "Shadow key refused." }, statusCode: StatusCodes.Status401Unauthorized);

    if (vaultUnresolved)
        return Results.Json(new { detail = "The SQL connection has not resolved from Key Vault." }, statusCode: StatusCodes.Status503ServiceUnavailable);

    if (database is null)
    {
        return Results.Json(
            new { detail = "Shadow comparison needs the SQL estate. The procedure is not in the in-memory path." },
            statusCode: StatusCodes.Status409Conflict);
    }

    try
    {
        var row = await database.WriteShadowAsync(invoiceNumber, cancellationToken);
        return Results.Ok(row);
    }
    catch (ProcedureRetiredException exception)
    {
        return Results.Json(new { detail = exception.Message }, statusCode: StatusCodes.Status409Conflict);
    }
    catch (InvoiceNotFoundException exception)
    {
        return Results.Json(new { detail = exception.Message }, statusCode: StatusCodes.Status404NotFound);
    }
});

app.Run();

async Task<IResult> Match(string? invoiceNumber, CancellationToken cancellationToken)
{
    if (string.IsNullOrWhiteSpace(invoiceNumber))
        return Results.BadRequest(new { detail = "invoiceNumber is required." });

    if (vaultUnresolved)
        return Results.Json(new { detail = "The SQL connection has not resolved from Key Vault." }, statusCode: StatusCodes.Status503ServiceUnavailable);

    try
    {
        if (database is null)
        {
            if (!string.Equals(invoiceNumber, CalloutEstate.InvoiceNumber, StringComparison.Ordinal))
                return Results.NotFound(new { detail = "In-memory estate only holds INV-1001." });

            return Results.Ok(MatchCalculator.Decide(invoiceNumber, CalloutEstate.InvoiceLines()));
        }

        return Results.Ok(await database.MatchFromServiceAsync(invoiceNumber, cancellationToken));
    }
    catch (InvoiceNotFoundException exception)
    {
        return Results.Json(new { detail = exception.Message }, statusCode: StatusCodes.Status404NotFound);
    }
}

static bool VaultUnresolved(string? value) =>
    value?.Contains("@Microsoft.KeyVault", StringComparison.Ordinal) == true;

internal sealed record MatchRequest(string InvoiceNumber);