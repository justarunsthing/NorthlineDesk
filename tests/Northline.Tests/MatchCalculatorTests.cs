using Northline.Match;

namespace Northline.Tests;

public sealed class MatchCalculatorTests
{
    [Fact]
    public void Partial_receipt_approves_six_callouts()
    {
        var decision = MatchCalculator.Decide(CalloutEstate.InvoiceNumber, CalloutEstate.InvoiceLines());

        Assert.Equal(612.00m, decision.Approved);
        Assert.Equal(6, decision.Unmatched);
        Assert.Equal("match-service", decision.DecisionPath);
    }

    [Fact]
    public void Estate_arithmetic_is_exact()
    {
        var lines = CalloutEstate.InvoiceLines();
        var invoiced = lines.Sum(line => line.QuantityInvoiced * line.UnitPrice);
        var received = lines.Sum(line => line.QuantityReceived * line.UnitPrice);

        Assert.Equal(12, lines.Count);
        Assert.Equal(1224.00m, invoiced);
        Assert.Equal(612.00m, received);
    }
}