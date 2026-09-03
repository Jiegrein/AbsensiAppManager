$dataSource = "ep-bitter-queen-b3amtxyx-pooler.c-4.ap-southeast-1.aws.neon.tech";
$port = "5432";
$initialCatalog = "AbsensiWebApiMigration";
$userId = "neondb_owner";
$password = "npg_2yl4nqWtLhZP";
$provider = "Npgsql.EntityFrameworkCore.PostgreSQL";
$entityFolderPath = "Entities";

$connectionString = "Host=$($dataSource); Port=$($port); Database=$($initialCatalog); Username=$($userId); Password=$($password); Timeout=30; MinPoolSize=1; MaxPoolSize=20";
$dbContextName = "AbsensiAppDbContext";

cd Entities
Remove-Item *.cs;
cd ..
dotnet ef dbcontext scaffold $connectionString $provider -d -f -c $dbContextName -v -o $entityFolderPath;