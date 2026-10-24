using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Globalization;

namespace NorthlineDesk
{
    public partial class PurchaseOrderPage : DeskPage
    {
        protected static readonly CultureInfo Pounds = CultureInfo.GetCultureInfo("en-GB");

        protected string OrderNumber = "PO-1001";
        protected string Supplier = "";
        protected string Depot = "";
        protected string Approver = "";
        protected string ApprovedOn = "";
        protected string InvoicedText = "";
        protected string ReceivedText = "";
        protected List<CalloutRow> Lines = new List<CalloutRow>();

        protected void Page_Load(object sender, EventArgs e)
        {
            OrderNumber = ReadQuery(Request.QueryString["po"], "PO-1001");

            try
            {
                LoadOrder();
            }
            catch (Exception exception)
            {
                Fail(exception.Message);
            }
        }

        private void LoadOrder()
        {
            if (string.IsNullOrWhiteSpace(ConnectionString) || ConnectionString.Contains("@Microsoft.KeyVault"))
                throw new InvalidOperationException("The desk cannot read SqlConnection. Check the managed identity and the vault.");

            const string sql = @"
                SELECT s.Name, po.Depot, a.Approver, a.ApprovedOn,
                       pol.Description, pol.QuantityOrdered, pol.UnitPrice,
                       ISNULL((SELECT SUM(rl.QuantityReceived) FROM dbo.ReceiptLine rl WHERE rl.PurchaseOrderLineId = pol.PurchaseOrderLineId), 0),
                       ISNULL((SELECT SUM(il.QuantityInvoiced) FROM dbo.InvoiceLine il WHERE il.PurchaseOrderLineId = pol.PurchaseOrderLineId), 0)
                FROM dbo.PurchaseOrder po
                INNER JOIN dbo.Supplier s ON s.SupplierId = po.SupplierId
                INNER JOIN dbo.PurchaseOrderLine pol ON pol.PurchaseOrderId = po.PurchaseOrderId
                LEFT JOIN dbo.Approval a ON a.PurchaseOrderId = po.PurchaseOrderId
                WHERE po.Number = @Number
                ORDER BY pol.LineNumber;";

            decimal invoiced = 0m;
            decimal received = 0m;

            using (var connection = new SqlConnection(ConnectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.Add("@Number", System.Data.SqlDbType.NVarChar, 32).Value = OrderNumber;
                connection.Open();

                using (var reader = command.ExecuteReader())
                {
                    var any = false;

                    while (reader.Read())
                    {
                        any = true;
                        Supplier = reader.GetString(0);
                        Depot = reader.GetString(1);

                        if (!reader.IsDBNull(2))
                        {
                            Approver = reader.GetString(2);
                            ApprovedOn = reader.GetDateTime(3).ToString("d MMMM yyyy", Pounds);
                        }

                        var ordered = reader.GetDecimal(5);
                        var price = reader.GetDecimal(6);
                        var got = reader.GetDecimal(7);
                        var billed = reader.GetDecimal(8);

                        invoiced += billed * price;
                        received += got * price;

                        Lines.Add(new CalloutRow(reader.GetString(4), ordered, got, price));
                    }

                    if (!any)
                        throw new InvalidOperationException("No purchase order " + OrderNumber + ".");
                }
            }

            InvoicedText = invoiced.ToString("C", Pounds);
            ReceivedText = received.ToString("C", Pounds);
        }

        protected sealed class CalloutRow
        {
            public CalloutRow(string description, decimal quantityOrdered, decimal quantityReceived, decimal unitPrice)
            {
                Description = description;
                QuantityOrdered = quantityOrdered;
                QuantityReceived = quantityReceived;
                UnitPrice = unitPrice;
            }

            public string Description { get; private set; }
            public decimal QuantityOrdered { get; private set; }
            public decimal QuantityReceived { get; private set; }
            public decimal UnitPrice { get; private set; }
        }
    }
}