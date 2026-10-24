namespace Northline.Match;

public sealed record CalloutLine(
    int LineNumber,
    string Description,
    decimal UnitPrice,
    decimal QuantityInvoiced,
    decimal QuantityReceived);