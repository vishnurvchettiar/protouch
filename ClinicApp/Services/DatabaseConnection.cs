using Npgsql;

namespace ClinicApp.Services;

/// <summary>Turns the DATABASE_URL that Render provides into a connection string Npgsql understands.</summary>
public static class DatabaseConnection
{
    public static string? PostgresFromEnvironment(string? databaseUrl, string? configured)
    {
        if (!string.IsNullOrWhiteSpace(databaseUrl)) return FromUrl(databaseUrl.Trim());
        if (!string.IsNullOrWhiteSpace(configured)) return configured.Trim();
        return null;
    }

    public static string FromUrl(string url)
    {
        // Already a key=value connection string.
        if (!url.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) &&
            !url.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
            return url;

        var uri = new Uri(url);
        var userInfo = uri.UserInfo.Split(':', 2);
        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.Port > 0 ? uri.Port : 5432,
            Database = uri.AbsolutePath.TrimStart('/'),
            Username = Uri.UnescapeDataString(userInfo[0]),
            Password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : string.Empty,
            // Render's internal address needs no certificate check; external connections use TLS.
            SslMode = SslMode.Prefer,
            TrustServerCertificate = true,
            Pooling = true,
            MaxPoolSize = 20,
            KeepAlive = 30
        };
        return builder.ConnectionString;
    }
}
