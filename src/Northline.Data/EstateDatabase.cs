using Microsoft.Data.SqlClient;
using Northline.Match;

namespace Northline.Data;

public sealed class EstateDatabase
{
    private readonly string _connectionString;

    public EstateDatabase(string connectionString)
    {
        _connectionString = ConnectionStrings.ForDatabase(connectionString);
    }

    public async Task<IReadOnlyList<CalloutLine>> ReadLinesAsync(string invoiceNumber, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT
                pol.LineNumber,
                pol.Description,
                il.UnitPrice,
                il.QuantityInvoiced,
                ISNULL((
                    SELECT SUM(rl.QuantityReceived)
                    FROM dbo.ReceiptLine AS rl
                    WHERE rl.PurchaseOrderLineId = pol.PurchaseOrderLineId
                ), 0) AS QuantityReceived
            FROM dbo.Invoice AS i
            INNER JOIN dbo.InvoiceLine AS il ON il.InvoiceId = i.InvoiceId
            INNER JOIN dbo.PurchaseOrderLine AS pol ON pol.PurchaseOrderLineId = il.PurchaseOrderLineId
            WHERE i.Number = @InvoiceNumber
            ORDER BY pol.LineNumber;
            """;

        var lines = new List<CalloutLine>();

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add(InvoiceParameter(invoiceNumber));

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            lines.Add(new CalloutLine(
                reader.GetInt32(0),
                reader.GetString(1),
                reader.GetDecimal(2),
                reader.GetDecimal(3),
                reader.GetDecimal(4)));
        }

        if (lines.Count == 0)
            throw new InvoiceNotFoundException(invoiceNumber);

        return lines;
    }

    public async Task<MatchDecision> MatchFromServiceAsync(string invoiceNumber, CancellationToken cancellationToken)
    {
        var lines = await ReadLinesAsync(invoiceNumber, cancellationToken);

        return MatchCalculator.Decide(invoiceNumber, lines);
    }

    public async Task<MatchDecision> MatchFromProcedureAsync(string invoiceNumber, CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand("dbo.usp_MatchInvoice", connection)
        {
            CommandType = System.Data.CommandType.StoredProcedure,
        };

        command.Parameters.Add(InvoiceParameter(invoiceNumber));

        try
        {
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            if (!await reader.ReadAsync(cancellationToken))
                throw new InvoiceNotFoundException(invoiceNumber);

            return new MatchDecision(
                reader.GetString(reader.GetOrdinal("InvoiceNumber")),
                reader.GetDecimal(reader.GetOrdinal("Approved")),
                reader.GetInt32(reader.GetOrdinal("Unmatched")),
                "stored-procedure");
        }
        catch (SqlException exception) when (exception.Number is 2812 or 208)
        {
            throw new ProcedureRetiredException();
        }
    }

    public async Task<ShadowRow> WriteShadowAsync(string invoiceNumber, CancellationToken cancellationToken)
    {
        var legacy = await MatchFromProcedureAsync(invoiceNumber, cancellationToken);
        var service = await MatchFromServiceAsync(invoiceNumber, cancellationToken);
        var ticket = await ReadTicketAsync(invoiceNumber, cancellationToken);
        var blocked = ShadowGate.BlocksPromotion(
            legacy.Approved,
            legacy.Unmatched,
            service.Approved,
            service.Unmatched,
            ticket);

        const string insert = """
            INSERT INTO dbo.MatchShadow (
                InvoiceNumber,
                LegacyApproved,
                ServiceApproved,
                LegacyUnmatched,
                ServiceUnmatched,
                ExplainedByTicket)
            VALUES (
                @InvoiceNumber,
                @LegacyApproved,
                @ServiceApproved,
                @LegacyUnmatched,
                @ServiceUnmatched,
                @Ticket);
            """;

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand(insert, connection);
        command.Parameters.Add(InvoiceParameter(invoiceNumber));
        command.Parameters.Add(new SqlParameter("@LegacyApproved", legacy.Approved));
        command.Parameters.Add(new SqlParameter("@ServiceApproved", service.Approved));
        command.Parameters.Add(new SqlParameter("@LegacyUnmatched", legacy.Unmatched));
        command.Parameters.Add(new SqlParameter("@ServiceUnmatched", service.Unmatched));
        command.Parameters.Add(new SqlParameter("@Ticket", (object?)ticket ?? DBNull.Value));

        await command.ExecuteNonQueryAsync(cancellationToken);

        return new ShadowRow(
            invoiceNumber,
            legacy.Approved,
            legacy.Unmatched,
            service.Approved,
            service.Unmatched,
            ticket,
            blocked);
    }

    public async Task<PurchaseOrderDocument?> ReadPurchaseOrderAsync(string number, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT
                po.Number,
                s.Name,
                po.Depot,
                pol.LineNumber,
                pol.Description,
                pol.QuantityOrdered,
                pol.UnitPrice,
                ISNULL((
                    SELECT SUM(rl.QuantityReceived)
                    FROM dbo.ReceiptLine AS rl
                    WHERE rl.PurchaseOrderLineId = pol.PurchaseOrderLineId
                ), 0) AS QuantityReceived
            FROM dbo.PurchaseOrder AS po
            INNER JOIN dbo.Supplier AS s ON s.SupplierId = po.SupplierId
            INNER JOIN dbo.PurchaseOrderLine AS pol ON pol.PurchaseOrderId = po.PurchaseOrderId
            WHERE po.Number = @Number
            ORDER BY pol.LineNumber;
            """;

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add(new SqlParameter("@Number", System.Data.SqlDbType.NVarChar, 32) { Value = number });
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        string? supplier = null;
        string? depot = null;
        var lines = new List<PurchaseOrderDocumentLine>();

        while (await reader.ReadAsync(cancellationToken))
        {
            supplier = reader.GetString(1);
            depot = reader.GetString(2);
            lines.Add(new PurchaseOrderDocumentLine(
                reader.GetInt32(3),
                reader.GetString(4),
                reader.GetDecimal(5),
                reader.GetDecimal(6),
                reader.GetDecimal(7)));
        }

        if (supplier is null || depot is null)
            return null;

        return new PurchaseOrderDocument(number, supplier, depot, lines);
    }

    private async Task<string?> ReadTicketAsync(string invoiceNumber, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT Ticket
            FROM dbo.MatchExplanation
            WHERE InvoiceNumber = @InvoiceNumber;
            """;

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add(InvoiceParameter(invoiceNumber));

        var value = await command.ExecuteScalarAsync(cancellationToken);

        return value is string ticket ? ticket : null;
    }

    private static SqlParameter InvoiceParameter(string invoiceNumber)
    {
        return new SqlParameter("@InvoiceNumber", System.Data.SqlDbType.NVarChar, 32) { Value = invoiceNumber };
    }
}

public sealed record PurchaseOrderDocument(
    string Number,
    string Supplier,
    string Depot,
    IReadOnlyList<PurchaseOrderDocumentLine> Lines);

public sealed record PurchaseOrderDocumentLine(
    int LineNumber,
    string Description,
    decimal QuantityOrdered,
    decimal UnitPrice,
    decimal QuantityReceived);