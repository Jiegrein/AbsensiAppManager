using System.Threading.Tasks;
using AbsensiAppWebApi.DB.Migrations;
using AbsensiAppWebApi.Tests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AbsensiAppWebApi.Tests;

[Collection("api")]
public class MigratorTests(ApiFixture fixture)
{
    [Fact]
    public async Task Startup_migration_applied_all_scripts_once_and_a_rerun_applies_nothing()
    {
        using var scope = fixture.CreateScope();
        var migrator = scope.ServiceProvider.GetRequiredService<IDatabaseMigrator>();
        using var db = fixture.CreateDbContext();

        var appliedOnRerun = await migrator.MigrateAsync();

        Assert.Equal(0, appliedOnRerun);
        Assert.Equal(4, await db.ScanEnums.CountAsync());
        Assert.Contains(await db.Projects.ToListAsync(), p => p.ProjectId == ApiFixture.CikandeProjectId && p.HourOffsetGmt == 7);
        Assert.Equal(5L, await CountAppliedScripts(db));
    }

    private static async Task<long> CountAppliedScripts(DbContext db)
    {
        var connection = db.Database.GetDbConnection();
        await connection.OpenAsync();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT count(*) FROM schema_migration";
        return (long)await command.ExecuteScalarAsync();
    }
}
