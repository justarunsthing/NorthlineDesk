using Microsoft.Data.SqlClient;

namespace Northline.Data;

public static class ConnectionStrings
{
    public const string DatabaseName = "NorthlineDesk";

    public static string ForDatabase(string connectionString)
    {
        var builder = new SqlConnectionStringBuilder(connectionString)
        {
            InitialCatalog = DatabaseName,
        };

        return builder.ConnectionString;
    }

    public static string ForMaster(string connectionString)
    {
        var builder = new SqlConnectionStringBuilder(connectionString)
        {
            InitialCatalog = "master",
        };

        return builder.ConnectionString;
    }
}