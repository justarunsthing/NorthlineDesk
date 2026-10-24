using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Web.UI;

namespace NorthlineDesk
{
    public partial class MatchPage : DeskPage
    {
        private static readonly HttpClient Http = new HttpClient();
        private static readonly CultureInfo Pounds = CultureInfo.GetCultureInfo("en-GB");

        protected string InvoiceNumber = "INV-1001";
        protected string DecisionPathCode = "";
        protected string DecisionPathLabel = "";
        protected string ApprovedText = "";
        protected int Unmatched;
        protected string PathNote = "";

        protected void Page_Load(object sender, EventArgs e)
        {
            InvoiceNumber = ReadQuery(Request.QueryString["invoice"], "INV-1001");
            // RegisterAsyncTask keeps the request context, so a failure can still set HTTP 500.
            RegisterAsyncTask(new PageAsyncTask(LoadMatch));
        }

        private async Task LoadMatch(CancellationToken cancellationToken)
        {
            var mode = System.Configuration.ConfigurationManager.AppSettings["MatchMode"] ?? "Procedure";

            try
            {
                if (string.Equals(mode, "Api", StringComparison.OrdinalIgnoreCase))
                    await LoadFromService(cancellationToken);
                else
                    LoadFromProcedure();
            }
            catch (ProcedureGoneException)
            {
                Fail("The stored procedure is gone. This slot is still on the legacy decision.");
            }
            catch (Exception exception)
            {
                Fail(exception.Message);
            }
        }

        // MatchMode is not a deployment-slot setting, so a swap carries it.
        // Procedure is the production slot before the swap. Api is the staging slot.
        private void LoadFromProcedure()
        {
            if (string.IsNullOrWhiteSpace(ConnectionString) || ConnectionString.Contains("@Microsoft.KeyVault"))
                throw new InvalidOperationException("The desk cannot read SqlConnection. Check the managed identity and the vault.");

            using (var connection = new SqlConnection(ConnectionString))
            using (var command = new SqlCommand("dbo.usp_MatchInvoice", connection))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.Add("@InvoiceNumber", SqlDbType.NVarChar, 32).Value = InvoiceNumber;
                connection.Open();
                try
                {
                    using (var reader = command.ExecuteReader())
                    {
                        if (!reader.Read())
                            throw new InvalidOperationException("No invoice " + InvoiceNumber + ".");

                        Show("stored-procedure", "stored procedure", reader.GetDecimal(reader.GetOrdinal("Approved")), reader.GetInt32(reader.GetOrdinal("Unmatched")));
                    }
                }
                catch (SqlException exception)
                {
                    if (exception.Number == 2812 || exception.Number == 208)
                        throw new ProcedureGoneException();
                    throw;
                }
            }

            PathNote = "This is the legacy decision. It does not read the receipt.";
        }

        private async Task LoadFromService(CancellationToken cancellationToken)
        {
            var url = System.Configuration.ConfigurationManager.AppSettings["MatchApiUrl"];

            if (string.IsNullOrWhiteSpace(url))
                throw new InvalidOperationException("MatchApiUrl is not set.");

            var body = new StringContent("{\"invoiceNumber\":\"" + InvoiceNumber + "\"}", Encoding.UTF8, "application/json");
            var response = await Http.PostAsync(url, body, cancellationToken);
            var json = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException("Match service returned " + (int)response.StatusCode + ".");

            // The service writes camelCase: approved, unmatched.
            var payload = new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(json);

            Show("match-service", "match service", Convert.ToDecimal(payload["approved"], CultureInfo.InvariantCulture), Convert.ToInt32(payload["unmatched"], CultureInfo.InvariantCulture));
            PathNote = "This is the match service. It approves only the callouts that were received.";
        }

        private void Show(string code, string label, decimal approved, int unmatched)
        {
            DecisionPathCode = code;
            DecisionPathLabel = label;
            ApprovedText = approved.ToString("C", Pounds);
            Unmatched = unmatched;
        }

        private sealed class ProcedureGoneException : Exception
        {
        }
    }
}