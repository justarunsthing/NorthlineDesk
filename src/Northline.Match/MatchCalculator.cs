namespace Northline.Match;

public static class MatchCalculator
{
    public const string DecisionPath = "match-service";

    public static MatchDecision Decide(string invoiceNumber, IReadOnlyList<CalloutLine> lines)
    {
        ArgumentNullException.ThrowIfNull(lines);

        decimal approved = 0m;
        var unmatched = 0;

        foreach (var line in lines)
        {
            var invoiced = line.QuantityInvoiced < 0m ? 0m : line.QuantityInvoiced;
            var received = line.QuantityReceived < 0m ? 0m : line.QuantityReceived;
            var covered = invoiced < received ? invoiced : received;
            approved += covered * line.UnitPrice;

            if (received < invoiced)
                unmatched++;
        }

        return new MatchDecision(invoiceNumber, approved, unmatched, DecisionPath);
    }
}