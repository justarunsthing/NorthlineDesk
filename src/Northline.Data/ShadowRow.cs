namespace Northline.Data;

public sealed record ShadowRow(
    string InvoiceNumber,
    decimal LegacyApproved,
    int LegacyUnmatched,
    decimal ServiceApproved,
    int ServiceUnmatched,
    string? Ticket,
    bool PromotionBlocked);