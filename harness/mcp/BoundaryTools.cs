using Northline.Match;

namespace Northline.Mcp;

public static class BoundaryTools
{
    public static IReadOnlyList<string> Names { get; } = ["match_invoice", "describe_purchase_order"];

    public static ToolResult Call(string name, IReadOnlyDictionary<string, string?> arguments)
    {
        foreach (var key in arguments.Keys)
        {
            if (IsClosed(key) || IsClosed(arguments[key]))
                return ToolResult.Refuse("That argument is outside the agent boundary. This server has no SQL tool, and it does not read approvals, requisitions, or the VAT rate.");
        }

        if (IsClosed(name))
            return ToolResult.Refuse("That tool is outside the agent boundary. This server has no SQL tool.");

        return name switch
        {
            "match_invoice" => Match(arguments),
            "describe_purchase_order" => Describe(arguments),
            _ => ToolResult.Refuse("Unknown tool " + name + "."),
        };
    }

    private static ToolResult Match(IReadOnlyDictionary<string, string?> arguments)
    {
        var invoice = arguments.TryGetValue("invoiceNumber", out var value) && !string.IsNullOrWhiteSpace(value)
            ? value
            : CalloutEstate.InvoiceNumber;

        if (!string.Equals(invoice, CalloutEstate.InvoiceNumber, StringComparison.Ordinal))
            return ToolResult.Refuse("This server only holds INV-1001.");

        var decision = MatchCalculator.Decide(invoice, CalloutEstate.InvoiceLines());

        return ToolResult.Ok(
            "Invoice " + decision.InvoiceNumber
            + ". Approved net " + decision.Approved.ToString("0.00")
            + ". Unmatched callouts " + decision.Unmatched
            + ". Decision path " + decision.DecisionPath + ".");
    }

    private static ToolResult Describe(IReadOnlyDictionary<string, string?> arguments)
    {
        var number = arguments.TryGetValue("purchaseOrderNumber", out var value) && !string.IsNullOrWhiteSpace(value)
            ? value
            : CalloutEstate.PurchaseOrderNumber;

        if (!string.Equals(number, CalloutEstate.PurchaseOrderNumber, StringComparison.Ordinal))
            return ToolResult.Refuse("This server only holds PO-1001.");

        var lines = CalloutEstate.InvoiceLines();
        var received = lines.Count(line => line.QuantityReceived > 0m);

        return ToolResult.Ok(
            "Purchase order " + number
            + " for " + CalloutEstate.Supplier
            + " at " + CalloutEstate.Depot
            + ". " + lines.Count + " callouts ordered, " + received + " received, unit price "
            + CalloutEstate.UnitPrice.ToString("0.00")
            + ". The clerk can see who approved the order on the desk. This tool cannot.");
    }

    private static bool IsClosed(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        return value.Contains("approval", StringComparison.OrdinalIgnoreCase)
            || value.Contains("requisition", StringComparison.OrdinalIgnoreCase)
            || value.Contains("vat", StringComparison.OrdinalIgnoreCase)
            || value.Contains("sql", StringComparison.OrdinalIgnoreCase);
    }
}