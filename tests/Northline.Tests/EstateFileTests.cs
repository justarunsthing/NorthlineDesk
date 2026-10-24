using Northline.Data;

namespace Northline.Tests;

public sealed class EstateFileTests
{
    [Fact]
    public void Legacy_procedure_does_not_read_the_receipt()
    {
        var procedure = MigrationRunner.ProcedureBatch();

        Assert.Contains("QuantityInvoiced", procedure, StringComparison.Ordinal);
        Assert.DoesNotContain("Receipt", procedure, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("CAST(0 AS int) AS Unmatched", procedure, StringComparison.Ordinal);
    }

    [Fact]
    public void Retirement_only_drops_the_procedure()
    {
        var sql = File.ReadAllText(Path.Combine(RepoPaths.Migrations(), "002_retire_procedure.sql"));

        Assert.Contains("DROP PROCEDURE IF EXISTS dbo.usp_MatchInvoice", sql, StringComparison.Ordinal);
        Assert.DoesNotContain("DROP TABLE", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Explanation_writes_the_ticket_and_not_the_approval()
    {
        var sql = File.ReadAllText(Path.Combine(RepoPaths.Explanations(), "partial-receipt.sql"));

        Assert.Contains("partial-receipt", sql, StringComparison.Ordinal);
        Assert.Contains("dbo.MatchExplanation", sql, StringComparison.Ordinal);
        Assert.DoesNotContain("dbo.Approval", sql, StringComparison.Ordinal);
        Assert.DoesNotContain("dbo.VatRate", sql, StringComparison.Ordinal);
    }
}