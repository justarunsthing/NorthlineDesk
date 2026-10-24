namespace Northline.Data;

public sealed class InvoiceNotFoundException : Exception
{
    public InvoiceNotFoundException(string invoiceNumber)
        : base("No invoice lines for " + invoiceNumber + ".")
    {
        InvoiceNumber = invoiceNumber;
    }

    public string InvoiceNumber { get; }
}