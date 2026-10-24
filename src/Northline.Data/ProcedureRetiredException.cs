namespace Northline.Data;

public sealed class ProcedureRetiredException : Exception
{
    public ProcedureRetiredException()
        : base("dbo.usp_MatchInvoice is gone. This path is still the legacy decision.")
    {
    }
}