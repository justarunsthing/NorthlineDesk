using System.Globalization;
using Microsoft.Data.SqlClient;

namespace Northline.Data;

public static class MigrationRunner
{
    public static async Task ApplyThroughAsync(string connectionString, int through, CancellationToken cancellationToken)
    {
        await EnsureDatabaseAsync(connectionString, cancellationToken);

        var database = ConnectionStrings.ForDatabase(connectionString);

        await EnsureHistoryAsync(database, cancellationToken);

        foreach (var migration in ListMigrations().Where(migration => migration.Version <= through))
        {
            if (await IsAppliedAsync(database, migration.Version, cancellationToken))
                continue;

            await ExecuteFileAsync(database, migration.Path, cancellationToken);
            await RecordAsync(database, migration, cancellationToken);
        }
    }

    public static async Task ApplyExplanationAsync(string connectionString, CancellationToken cancellationToken)
    {
        var database = ConnectionStrings.ForDatabase(connectionString);
        var path = Path.Combine(RepoPaths.Explanations(), "partial-receipt.sql");

        await ExecuteFileAsync(database, path, cancellationToken);
    }

    public static async Task ResetToVersionAsync(string connectionString, int version, CancellationToken cancellationToken)
    {
        if (version != 1)
            throw new ArgumentOutOfRangeException(nameof(version), "Reset only restores version 1, the procedure.");

        await EnsureDatabaseAsync(connectionString, cancellationToken);

        var database = ConnectionStrings.ForDatabase(connectionString);

        if (!await ObjectExistsAsync(database, "dbo.Invoice", "U", cancellationToken))
        {
            await ApplyThroughAsync(connectionString, 1, cancellationToken);
            return;
        }

        await ExecuteAsync(database, ProcedureBatch(), cancellationToken);

        if (await ObjectExistsAsync(database, "dbo.SchemaMigration", "U", cancellationToken))
            await ExecuteAsync(database, "DELETE FROM dbo.SchemaMigration WHERE Version > 1;", cancellationToken);
        if (await ObjectExistsAsync(database, "dbo.MatchShadow", "U", cancellationToken))
            await ExecuteAsync(database, "DELETE FROM dbo.MatchShadow;", cancellationToken);
        if (await ObjectExistsAsync(database, "dbo.MatchExplanation", "U", cancellationToken))
            await ExecuteAsync(database, "DELETE FROM dbo.MatchExplanation;", cancellationToken);
    }

    public static async Task<int?> ProcedureObjectIdAsync(string connectionString, CancellationToken cancellationToken)
    {
        var database = ConnectionStrings.ForDatabase(connectionString);
        await using var connection = await OpenAsync(database, cancellationToken);
        await using var command = new SqlCommand(
            "SELECT OBJECT_ID(N'dbo.usp_MatchInvoice', N'P');",
            connection);

        var value = await command.ExecuteScalarAsync(cancellationToken);

        if (value is null || value is DBNull)
            return null;

        return Convert.ToInt32(value, CultureInfo.InvariantCulture);
    }

    public static string ProcedureBatch()
    {
        var path = Path.Combine(RepoPaths.Migrations(), "001_estate.sql");
        var batches = SqlBatches.Split(File.ReadAllText(path));
        var procedure = batches.LastOrDefault(batch =>
            batch.Contains("CREATE OR ALTER PROCEDURE", StringComparison.OrdinalIgnoreCase));

        if (procedure is null)
            throw new InvalidOperationException("001_estate.sql has no procedure batch.");

        return procedure;
    }

    private static async Task EnsureDatabaseAsync(string connectionString, CancellationToken cancellationToken)
    {
        await using var connection = await OpenAsync(ConnectionStrings.ForMaster(connectionString), cancellationToken);
        await using var command = new SqlCommand(
            "IF DB_ID(N'NorthlineDesk') IS NULL CREATE DATABASE NorthlineDesk;",
            connection);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task<bool> ObjectExistsAsync(
        string connectionString,
        string name,
        string type,
        CancellationToken cancellationToken)
    {
        await using var connection = await OpenAsync(connectionString, cancellationToken);
        await using var command = new SqlCommand("SELECT OBJECT_ID(@Name, @Type);", connection);
        command.Parameters.Add(new SqlParameter("@Name", name));
        command.Parameters.Add(new SqlParameter("@Type", type));

        var value = await command.ExecuteScalarAsync(cancellationToken);

        return value is int;
    }

    private static async Task EnsureHistoryAsync(string connectionString, CancellationToken cancellationToken)
    {
        const string sql = """
            IF OBJECT_ID(N'dbo.SchemaMigration', N'U') IS NULL
            CREATE TABLE dbo.SchemaMigration (
                Version int NOT NULL CONSTRAINT PK_SchemaMigration PRIMARY KEY,
                Name nvarchar(200) NOT NULL,
                AppliedAt datetime2 NOT NULL CONSTRAINT DF_SchemaMigration_AppliedAt DEFAULT SYSUTCDATETIME()
            );
            """;

        await ExecuteAsync(connectionString, sql, cancellationToken);
    }

    private static async Task<bool> IsAppliedAsync(string connectionString, int version, CancellationToken cancellationToken)
    {
        await using var connection = await OpenAsync(connectionString, cancellationToken);
        await using var command = new SqlCommand(
            "SELECT 1 FROM dbo.SchemaMigration WHERE Version = @Version;",
            connection);
        command.Parameters.Add(new SqlParameter("@Version", version));

        var value = await command.ExecuteScalarAsync(cancellationToken);

        return value is not null;
    }

    private static async Task RecordAsync(string connectionString, Migration migration, CancellationToken cancellationToken)
    {
        await using var connection = await OpenAsync(connectionString, cancellationToken);
        await using var command = new SqlCommand(
            "INSERT INTO dbo.SchemaMigration (Version, Name) VALUES (@Version, @Name);",
            connection);

        command.Parameters.Add(new SqlParameter("@Version", migration.Version));
        command.Parameters.Add(new SqlParameter("@Name", migration.Name));

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task ExecuteFileAsync(string connectionString, string path, CancellationToken cancellationToken)
    {
        foreach (var batch in SqlBatches.Split(File.ReadAllText(path)))
            await ExecuteAsync(connectionString, batch, cancellationToken);
    }

    private static async Task ExecuteAsync(string connectionString, string sql, CancellationToken cancellationToken)
    {
        await using var connection = await OpenAsync(connectionString, cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    // The runner is outside Azure. A new firewall rule, and a paused serverless database, both refuse the first connection.
    private static async Task<SqlConnection> OpenAsync(string connectionString, CancellationToken cancellationToken)
    {
        for (var attempt = 1; ; attempt++)
        {
            var connection = new SqlConnection(connectionString);

            try
            {
                await connection.OpenAsync(cancellationToken);
                return connection;
            }
            catch (SqlException) when (attempt < 36)
            {
                await connection.DisposeAsync();
                await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
            }
            catch
            {
                await connection.DisposeAsync();
                throw;
            }
        }
    }

    private static IEnumerable<Migration> ListMigrations()
    {
        return Directory.EnumerateFiles(RepoPaths.Migrations(), "*.sql")
            .Select(path =>
            {
                var name = Path.GetFileName(path);
                var prefix = name.Split('_', 2)[0];
                if (!int.TryParse(prefix, out var version))
                    throw new InvalidOperationException("Migration file needs a numeric prefix: " + name);
                return new Migration(version, name, path);
            })
            .OrderBy(migration => migration.Version);
    }

    private sealed record Migration(int Version, string Name, string Path);
}