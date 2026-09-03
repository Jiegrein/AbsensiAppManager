using System.Threading;
using System.Threading.Tasks;

namespace AbsensiAppWebApi.DB.Migrations;

public interface IDatabaseMigrator
{
    /// <summary>Applies every pending migration script and returns how many were applied.</summary>
    Task<int> MigrateAsync(CancellationToken cancellationToken = default);
}
