using System;
using System.IO;
using System.Threading.Tasks;
using AbsensiAppWebApi.DB.Entities;
using AbsensiAppWebApi.DB.Migrations;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Xunit;

namespace AbsensiAppWebApi.Tests.Fixtures;

/// <summary>
/// Boots the real API (its own Startup, its own appsettings) against a throwaway PostgreSQL
/// container, with the schema built by the app's own SQL script migrator. Shared by every
/// test in the "api" collection, so tests must create their own workers rather than rely on
/// a clean database.
/// </summary>
public sealed class ApiFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer postgres = new PostgreSqlBuilder()
        .WithImage("postgres:17-alpine")
        .Build();

    /// <summary>Seeded by "3. Seeding for testing.sql".</summary>
    public static readonly Guid CikandeProjectId = Guid.Parse("142554ad-1df6-4902-a29a-3e948a706b6f");

    public async Task InitializeAsync()
    {
        await postgres.StartAsync();
        // Startup resolves the database from configuration; CreateDefaultBuilder reads this variable.
        Environment.SetEnvironmentVariable("DATABASE_URL", postgres.GetConnectionString());
        // AdminService.CreateExcel writes a copy under the temp folder and does not create the directory.
        Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "temp", "excel"));

        using var scope = Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<IDatabaseMigrator>().MigrateAsync();
    }

    public new async Task DisposeAsync()
    {
        await base.DisposeAsync();
        await postgres.DisposeAsync();
    }

    public IServiceScope CreateScope() => Services.CreateScope();

    public AbsensiAppDbContext CreateDbContext() => Services.CreateScope().ServiceProvider.GetRequiredService<AbsensiAppDbContext>();
}

[CollectionDefinition("api")]
public sealed class ApiCollection : ICollectionFixture<ApiFixture>
{
}
