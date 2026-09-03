using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AbsensiAppWebApi.DB.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AbsensiAppWebApi.DB.Migrations;

/// <summary>
/// Applies raw SQL migration scripts in order, once each. Applied script names are recorded in
/// <c>schema_migration</c> so re-running on an already migrated database is a no-op. Each script
/// runs inside its own transaction, so a failing script leaves the database at the previous version.
/// </summary>
public sealed class SqlScriptMigrator : IDatabaseMigrator
{
    private const string HistoryTable = "schema_migration";

    private const string EnsureHistoryTableSql = @"
CREATE TABLE IF NOT EXISTS " + HistoryTable + @" (
    name TEXT PRIMARY KEY,
    applied_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);";

    private const string SelectAppliedSql = "SELECT name FROM " + HistoryTable + ";";
    private const string InsertAppliedSql = "INSERT INTO " + HistoryTable + " (name) VALUES (@name);";

    private readonly AbsensiAppDbContext db;
    private readonly ISqlScriptSource scriptSource;
    private readonly ILogger<SqlScriptMigrator> logger;

    public SqlScriptMigrator(AbsensiAppDbContext db, ISqlScriptSource scriptSource, ILogger<SqlScriptMigrator> logger)
    {
        this.db = db;
        this.scriptSource = scriptSource;
        this.logger = logger;
    }

    public async Task<int> MigrateAsync(CancellationToken cancellationToken = default)
    {
        var connection = db.Database.GetDbConnection();
        await connection.OpenAsync(cancellationToken);
        try
        {
            await ExecuteAsync(connection, EnsureHistoryTableSql, cancellationToken);
            var applied = await ReadAppliedAsync(connection, cancellationToken);

            var pending = scriptSource.GetScripts()
                .Where(script => !applied.Contains(script.Name))
                .ToList();

            if (pending.Count == 0)
            {
                logger.LogInformation("Database schema is up to date ({Count} scripts already applied).", applied.Count);
                return 0;
            }

            foreach (var script in pending)
            {
                await ApplyAsync(connection, script, cancellationToken);
            }

            logger.LogInformation("Applied {Count} database migration script(s).", pending.Count);
            return pending.Count;
        }
        finally
        {
            await connection.CloseAsync();
        }
    }

    private async Task ApplyAsync(DbConnection connection, SqlScript script, CancellationToken cancellationToken)
    {
        logger.LogInformation("Applying database migration script {Script}.", script.Name);

        using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            await ExecuteAsync(connection, script.Sql, cancellationToken, transaction);
            await ExecuteAsync(connection, InsertAppliedSql, cancellationToken, transaction, ("@name", script.Name));
            await transaction.CommitAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            throw new InvalidOperationException(
                $"Database migration script '{script.Name}' failed; the transaction was rolled back.", ex);
        }
    }

    private static async Task<HashSet<string>> ReadAppliedAsync(DbConnection connection, CancellationToken cancellationToken)
    {
        using var command = connection.CreateCommand();
        command.CommandText = SelectAppliedSql;

        var applied = new HashSet<string>(StringComparer.Ordinal);
        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            applied.Add(reader.GetString(0));
        }
        return applied;
    }

    private static async Task ExecuteAsync(
        DbConnection connection,
        string sql,
        CancellationToken cancellationToken,
        DbTransaction transaction = null,
        params (string Name, string Value)[] parameters)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Transaction = transaction;
        foreach (var (name, value) in parameters)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = name;
            parameter.Value = value;
            parameter.DbType = DbType.String;
            command.Parameters.Add(parameter);
        }
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
