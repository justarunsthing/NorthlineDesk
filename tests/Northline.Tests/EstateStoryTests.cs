using Northline.Data;
using Northline.Match;

namespace Northline.Tests;

public sealed class EstateStoryTests
{
    [Fact]
    public async Task Cutover_story_against_sql()
    {
        var connection = Environment.GetEnvironmentVariable("NORTHLINE_SQL");

        if (string.IsNullOrWhiteSpace(connection))
        {
            if (Environment.GetEnvironmentVariable("REQUIRE_SQL") == "1")
                Assert.Fail("NORTHLINE_SQL is required in CI.");
            return;
        }

        await MigrationRunner.ResetToVersionAsync(connection, 1, CancellationToken.None);
        await MigrationRunner.ApplyThroughAsync(connection, through: 1, CancellationToken.None);

        var database = new EstateDatabase(connection);

        var legacy = await database.MatchFromProcedureAsync(CalloutEstate.InvoiceNumber, CancellationToken.None);
        var service = await database.MatchFromServiceAsync(CalloutEstate.InvoiceNumber, CancellationToken.None);

        Assert.Equal(1224.00m, legacy.Approved);
        Assert.Equal(0, legacy.Unmatched);
        Assert.Equal(612.00m, service.Approved);
        Assert.Equal(6, service.Unmatched);

        var blocked = await database.WriteShadowAsync(CalloutEstate.InvoiceNumber, CancellationToken.None);

        Assert.True(blocked.PromotionBlocked);
        Assert.Null(blocked.Ticket);

        await MigrationRunner.ApplyExplanationAsync(connection, CancellationToken.None);

        var explained = await database.WriteShadowAsync(CalloutEstate.InvoiceNumber, CancellationToken.None);

        Assert.False(explained.PromotionBlocked);
        Assert.Equal(ShadowGate.Ticket, explained.Ticket);

        await MigrationRunner.ApplyThroughAsync(connection, through: 2, CancellationToken.None);

        var procedureId = await MigrationRunner.ProcedureObjectIdAsync(connection, CancellationToken.None);

        Assert.Null(procedureId);

        var after = await database.MatchFromServiceAsync(CalloutEstate.InvoiceNumber, CancellationToken.None);

        Assert.Equal(612.00m, after.Approved);

        await Assert.ThrowsAsync<ProcedureRetiredException>(() =>
            database.MatchFromProcedureAsync(CalloutEstate.InvoiceNumber, CancellationToken.None));

        await MigrationRunner.ResetToVersionAsync(connection, 1, CancellationToken.None);

        var restored = await database.MatchFromProcedureAsync(CalloutEstate.InvoiceNumber, CancellationToken.None);

        Assert.Equal(1224.00m, restored.Approved);
    }
}