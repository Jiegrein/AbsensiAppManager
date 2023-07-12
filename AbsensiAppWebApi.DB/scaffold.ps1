$dataSource = "localhost";
$port = "5432";
$initialCatalog = "AbsensiWebApiMigration";
$userId = "postgres";
$password = "admin";
$provider = "Npgsql.EntityFrameworkCore.PostgreSQL";
$entityFolderPath = "Entities";

$connectionString = "Host=$($dataSource); Port=$($port); Database=$($initialCatalog); Username=$($userId); Password=$($password); Timeout=30; MinPoolSize=1; MaxPoolSize=20";
$dbContextName = "AbsensiAppDbContext";

cd Entities
Remove-Item *.cs;
cd ..
dotnet ef dbcontext scaffold $connectionString $provider -d -f -c $dbContextName -v -o $entityFolderPath;