using System;
using System.Web;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace AbsensiAppWebApi.Infrastructure;

/// <summary>
/// Resolves the PostgreSQL connection string from configuration. The <c>DATABASE_URL</c> setting
/// (URL form, e.g. <c>postgresql://user:pass@host/db?sslmode=require</c>, as issued by Neon) takes
/// precedence; <c>ConnectionStrings:AbsensiAppDb</c> (key/value form) is the local-development fallback.
/// There is deliberately no hardcoded default: a missing setting fails fast at startup.
/// </summary>
public static class DatabaseConnectionStringResolver
{
    public const string DatabaseUrlKey = "DATABASE_URL";
    public const string ConnectionStringName = "AbsensiAppDb";
    private const int DefaultPostgresPort = 5432;

    public static string Resolve(IConfiguration configuration)
    {
        var databaseUrl = configuration[DatabaseUrlKey];
        if (!string.IsNullOrWhiteSpace(databaseUrl))
        {
            return FromDatabaseUrl(databaseUrl);
        }

        var connectionString = configuration.GetConnectionString(ConnectionStringName);
        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            return connectionString;
        }

        throw new InvalidOperationException(
            $"No database configured. Set the '{DatabaseUrlKey}' environment variable " +
            $"or 'ConnectionStrings:{ConnectionStringName}' in appsettings.");
    }

    private static string FromDatabaseUrl(string databaseUrl)
    {
        var uri = new Uri(databaseUrl);
        var userInfo = uri.UserInfo.Split(':', 2);
        var sslMode = ParseSslMode(HttpUtility.ParseQueryString(uri.Query)["sslmode"]);

        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.Port > 0 ? uri.Port : DefaultPostgresPort,
            Database = uri.AbsolutePath.Trim('/'),
            Username = Uri.UnescapeDataString(userInfo[0]),
            Password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : null,
            Pooling = true,
            SslMode = sslMode,
            // Hosted providers (Neon, Heroku) present certificates that are not always in the local store.
            TrustServerCertificate = sslMode == SslMode.Require,
        };
        return builder.ConnectionString;
    }

    // Maps libpq's sslmode values; hosted providers require TLS, so that is the default when absent.
    private static SslMode ParseSslMode(string value) => value?.ToLowerInvariant() switch
    {
        "disable" => SslMode.Disable,
        "prefer" => SslMode.Prefer,
        "verify-ca" => SslMode.VerifyCA,
        "verify-full" => SslMode.VerifyFull,
        _ => SslMode.Require,
    };
}
