using Northline.Data;

var connection = ReadOption(args, "--connection") ?? Environment.GetEnvironmentVariable("NORTHLINE_SQL");

if (string.IsNullOrWhiteSpace(connection))
{
    Console.Error.WriteLine("Set NORTHLINE_SQL or pass --connection.");
    return 1;
}

var through = int.TryParse(ReadOption(args, "--through"), out var parsed) ? parsed : 1;
var reset = args.Contains("--reset-to", StringComparer.Ordinal) && ReadOption(args, "--reset-to") == "1";
var assertRetired = args.Contains("--assert-retired", StringComparer.Ordinal);
var explain = args.Contains("--explain", StringComparer.Ordinal);

if (reset)
{
    await MigrationRunner.ResetToVersionAsync(connection, 1, CancellationToken.None);
    Console.WriteLine("Reset to version 1. The procedure is restored. Shadow rows and the explanation are cleared.");
}

if (explain)
{
    await MigrationRunner.ApplyExplanationAsync(connection, CancellationToken.None);
    Console.WriteLine("Recorded ticket partial-receipt against INV-1001.");
}

await MigrationRunner.ApplyThroughAsync(connection, through, CancellationToken.None);

Console.WriteLine("Applied migrations through " + through + ".");

if (assertRetired)
{
    var id = await MigrationRunner.ProcedureObjectIdAsync(connection, CancellationToken.None);
    if (id is not null)
    {
        Console.Error.WriteLine("dbo.usp_MatchInvoice still exists. OBJECT_ID is " + id + ".");
        return 1;
    }

    Console.WriteLine("OBJECT_ID(dbo.usp_MatchInvoice) is null.");
}

return 0;

static string? ReadOption(string[] args, string name)
{
    for (var i = 0; i < args.Length - 1; i++)
    {
        if (string.Equals(args[i], name, StringComparison.Ordinal))
            return args[i + 1];
    }

    return null;
}