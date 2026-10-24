using Northline.Match;

namespace Northline.Tests;

public sealed class ShadowGateTests
{
    [Fact]
    public void Unexplained_partial_receipt_blocks_promotion()
    {
        var blocked = ShadowGate.BlocksPromotion(1224.00m, 0, 612.00m, 6, ticket: null);

        Assert.True(blocked);
    }

    [Fact]
    public void The_support_ticket_explains_only_that_difference()
    {
        var blocked = ShadowGate.BlocksPromotion(1224.00m, 0, 612.00m, 6, ShadowGate.Ticket);

        Assert.False(blocked);
    }

    [Fact]
    public void A_one_penny_drift_stays_blocked_with_the_same_ticket()
    {
        var blocked = ShadowGate.BlocksPromotion(1224.00m, 0, 612.01m, 6, ShadowGate.Ticket);

        Assert.True(blocked);
    }

    [Fact]
    public void Matching_figures_do_not_need_a_ticket()
    {
        var blocked = ShadowGate.BlocksPromotion(612.00m, 6, 612.00m, 6, ticket: null);

        Assert.False(blocked);
    }
}