using System;
using System.Text.RegularExpressions;
using System.Web.UI;

namespace NorthlineDesk
{
    public class DeskPage : Page
    {
        protected static readonly Regex SafeNumber = new Regex("^[A-Z0-9-]{1,32}$", RegexOptions.CultureInvariant | RegexOptions.Compiled);

        protected string ConnectionString
        {
            get { return System.Configuration.ConfigurationManager.AppSettings["SqlConnection"] ?? string.Empty; }
        }

        protected void Fail(string message)
        {
            Response.StatusCode = 500;
            Response.TrySkipIisCustomErrors = true;
            ErrorMessage = message;
        }

        protected string ErrorMessage { get; private set; }

        protected bool HasError
        {
            get { return !string.IsNullOrEmpty(ErrorMessage); }
        }

        protected static string ReadQuery(string value, string fallback)
        {
            if (string.IsNullOrWhiteSpace(value))
                return fallback;

            var trimmed = value.Trim().ToUpperInvariant();

            return SafeNumber.IsMatch(trimmed) ? trimmed : fallback;
        }
    }
}