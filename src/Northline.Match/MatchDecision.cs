namespace Northline.Match;

public sealed record MatchDecision(
    string InvoiceNumber,
    decimal Approved,
    int Unmatched,
    string DecisionPath);