using Microsoft.Data.SqlClient;

namespace Tokenization.Tests.Shared.Fixtures;

internal static class SqlServerTestDependency
{
    private const string LocalComposeHost = "localhost,14333";
    private static readonly string LocalComposePassword =
        Environment.GetEnvironmentVariable("MSSQL_SA_PASSWORD") ?? "Your_strong_password123";

    public static async Task<string?> TryGetLocalComposeConnectionStringAsync(CancellationToken ct = default)
    {
        var connectionString = BuildConnectionString(LocalComposeHost, "master", connectTimeoutSeconds: 1);
        return await CanConnectAsync(connectionString, ct) ? connectionString : null;
    }

    public static bool ShouldReuseContainers()
    {
        return !string.Equals(Environment.GetEnvironmentVariable("CI"), "true", StringComparison.OrdinalIgnoreCase) &&
               !string.Equals(
                   Environment.GetEnvironmentVariable("TOKENIZATION_DISABLE_TESTCONTAINER_REUSE"),
                   "true",
                   StringComparison.OrdinalIgnoreCase);
    }

    private static async Task<bool> CanConnectAsync(string connectionString, CancellationToken ct)
    {
        try
        {
            await using var conn = new SqlConnection(connectionString);
            await conn.OpenAsync(ct);
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT 1";
            cmd.CommandTimeout = 1;
            await cmd.ExecuteScalarAsync(ct);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static string BuildConnectionString(string host, string database, int connectTimeoutSeconds)
    {
        return new SqlConnectionStringBuilder
        {
            DataSource = host,
            InitialCatalog = database,
            UserID = "sa",
            Password = LocalComposePassword,
            Encrypt = true,
            TrustServerCertificate = true,
            ConnectTimeout = connectTimeoutSeconds
        }.ConnectionString;
    }
}
