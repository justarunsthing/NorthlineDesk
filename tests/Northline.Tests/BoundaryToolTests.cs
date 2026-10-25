using Northline.Mcp;

namespace Northline.Tests;

public sealed class BoundaryToolTests
{
    [Fact]
    public void Match_tool_returns_the_spec_numbers()
    {
        var result = BoundaryTools.Call("match_invoice", new Dictionary<string, string?>());

        Assert.False(result.IsError);
        Assert.Contains("612.00", result.Text, StringComparison.Ordinal);
        Assert.Contains("Unmatched callouts 6", result.Text, StringComparison.Ordinal);
    }

    [Fact]
    public void Purchase_order_tool_hides_the_approver()
    {
        var result = BoundaryTools.Call("describe_purchase_order", new Dictionary<string, string?>());

        Assert.False(result.IsError);
        Assert.Contains("6 received", result.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("John Doe", result.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("REQ-1001", result.Text, StringComparison.Ordinal);
    }

    [Fact]
    public void A_sql_tool_is_refused()
    {
        var result = BoundaryTools.Call("query", new Dictionary<string, string?> { ["sql"] = "SELECT * FROM dbo.Approval" });

        Assert.True(result.IsError);
        Assert.Contains("no SQL tool", result.Text, StringComparison.Ordinal);
    }
}