namespace Northline.Match;

public static class ShadowGate
{
    public const decimal LegacyApproved = 1224.00m;
    public const decimal ServiceApproved = 612.00m;
    public const int LegacyUnmatched = 0;
    public const int ServiceUnmatched = 6;
    public const string Ticket = "partial-receipt";

    public static bool BlocksPromotion(
        decimal legacyApproved,
        int legacyUnmatched,
        decimal serviceApproved,
        int serviceUnmatched,
        string? ticket)
    {
        if (legacyApproved == serviceApproved && legacyUnmatched == serviceUnmatched)
            return false;

        return !IsTheKnownPartialReceipt(legacyApproved, legacyUnmatched, serviceApproved, serviceUnmatched, ticket);
    }

    public static bool IsTheKnownPartialReceipt(
        decimal legacyApproved,
        int legacyUnmatched,
        decimal serviceApproved,
        int serviceUnmatched,
        string? ticket)
    {
        return legacyApproved == LegacyApproved
            && legacyUnmatched == LegacyUnmatched
            && serviceApproved == ServiceApproved
            && serviceUnmatched == ServiceUnmatched
            && string.Equals(ticket, Ticket, StringComparison.Ordinal);
    }
}