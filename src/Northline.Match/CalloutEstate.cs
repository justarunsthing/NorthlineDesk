namespace Northline.Match;

/// <summary>
/// The in-memory copy of INV-1001. The SQL seed in db/migrations/001_estate.sql is the same estate.
/// </summary>
public static class CalloutEstate
{
    public const string InvoiceNumber = "INV-1001";
    public const string PurchaseOrderNumber = "PO-1001";
    public const string Supplier = "Heston Mechanical";
    public const string Depot = "Northline depot, Reading";
    public const decimal UnitPrice = 102.00m;
    public const int CalloutCount = 12;
    public const int ReceivedCount = 6;

    public static IReadOnlyList<CalloutLine> InvoiceLines()
    {
        var lines = new List<CalloutLine>(CalloutCount);

        for (var number = 1; number <= CalloutCount; number++)
        {
            lines.Add(new CalloutLine(
                number,
                "Callout " + number,
                UnitPrice,
                QuantityInvoiced: 1m,
                QuantityReceived: number <= ReceivedCount ? 1m : 0m));
        }

        return lines;
    }
}