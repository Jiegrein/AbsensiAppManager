using System;
using System.Data.Common;
using System.Web;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace AbsensiAppWebApi.Infrastructure;

/// <summary>
/// Resolves the PostgreSQL connection string from configuration. The <c>DATABASE_URL</c> setting takes
/// precedence and accepts either form Neon hands out: the libpq URL
/// (<c>postgresql://user:pass@host/db?sslmode=require</c>) or the .NET key/value string
/// (<c>Host=...;Username=...;SSL Mode=VerifyFull</c>). <c>ConnectionStrings:AbsensiAppDb</c> is the
/// local-development fallback. There is deliberately no hardcoded default: a missing setting fails fast.
/// </summary>
public static class DatabaseConnectionStringResolver
{
    public const string DatabaseUrlKey = "DATABASE_URL";
    public const string ConnectionStringName = "AbsensiAppDb";
    private const int DefaultPostgresPort = 5432;

    // Keywords Neon includes that Npgsql 6 does not recognise; it throws on unknown keys.
    // Channel binding is negotiated automatically by Npgsql when TLS is on, so dropping it is safe.
    private static readonly string[] UnsupportedKeywords = { "Channel Binding" };

    public static string Resolve(IConfiguration configuration)
    {
        var databaseUrl = configuration[DatabaseUrlKey];
        if (!string.IsNullOrWhiteSpace(databaseUrl))
        {
            return IsUrlForm(databaseUrl) ? FromDatabaseUrl(databaseUrl) : FromKeyValue(databaseUrl);
        }

        var connectionString = configuration.GetConnectionString(ConnectionStringName);
        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            return FromKeyValue(connectionString);
        }

        throw new InvalidOperationException(
            $"No database configured. Set the '{DatabaseUrlKey}' environment variable " +
            $"or 'ConnectionStrings:{ConnectionStringName}' in appsettings.");
    }

    private static bool IsUrlForm(string value) =>
        value.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) ||
        value.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase);

    private static string FromKeyValue(string connectionString)
    {
        var builder = new DbConnectionStringBuilder { ConnectionString = connectionString.Trim().Trim('"') };
        foreach (var keyword in UnsupportedKeywords)
        {
            builder.Remove(keyword);
        }
        return new NpgsqlConnectionStringBuilder(builder.ConnectionString).ConnectionString;
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
