using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;

class Program
{
    static string masterConn =
        "Server=(localdb)\\MSSQLLocalDB;Database=master;Trusted_Connection=True;";

    static string tempDb = "EfTempDb";

    static string tempConn =>
        $"Server=(localdb)\\MSSQLLocalDB;Database={tempDb};Trusted_Connection=True;";

    static void Main(string[] args)
    {
        var legacySqlFile = args[0];
        var currentConn = args[1];
        var outputDir = args[2];

        CreateDb();
        ExecuteSql(File.ReadAllText(legacySqlFile));

        using var legacyCtx = new LegacyDbContext(tempConn);
        using var currentCtx = new CurrentDbContext(currentConn);

        var legacyModel = legacyCtx.GetService<IDesignTimeModel>().Model;
        var currentModel = currentCtx.GetService<IDesignTimeModel>().Model;

        var services = new ServiceCollection()
            .AddEntityFrameworkSqlServer()
            .BuildServiceProvider();

        var differ = services.GetRequiredService<IMigrationsModelDiffer>();

        var operations = differ.GetDifferences(
            legacyModel.GetRelationalModel(),
            currentModel.GetRelationalModel()
        );

        var scaffolder = services.GetRequiredService<IMigrationsScaffolder>();

        var migration = scaffolder.ScaffoldMigration(
            name: "SchemaDiff",
            rootNamespace: "Migrations",
            subNamespace: "",
            language: "C#",
            activeProvider: "Microsoft.EntityFrameworkCore.SqlServer",
            operations: operations,
            targetModel: currentModel,
            lastModel: legacyModel
        );

        Directory.CreateDirectory(outputDir);

        File.WriteAllText(
            Path.Combine(outputDir, migration.MigrationId + ".cs"),
            migration.MigrationCode);

        File.WriteAllText(
            Path.Combine(outputDir, migration.MigrationId + ".Designer.cs"),
            migration.MetadataCode);

        DropDb();
    }

    static void CreateDb()
    {
        using var conn = new SqlConnection(masterConn);
        conn.Open();

        new SqlCommand($"IF DB_ID('{tempDb}') IS NOT NULL DROP DATABASE {tempDb}", conn)
            .ExecuteNonQuery();

        new SqlCommand($"CREATE DATABASE {tempDb}", conn)
            .ExecuteNonQuery();
    }

    static void ExecuteSql(string sql)
    {
        using var conn = new SqlConnection(tempConn);
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandTimeout = 0;
        cmd.CommandText = sql;
        cmd.ExecuteNonQuery();
    }

    static void DropDb()
    {
        using var conn = new SqlConnection(masterConn);
        conn.Open();

        new SqlCommand(
            $"ALTER DATABASE {tempDb} SET SINGLE_USER WITH ROLLBACK IMMEDIATE",
            conn).ExecuteNonQuery();

        new SqlCommand($"DROP DATABASE {tempDb}", conn)
            .ExecuteNonQuery();
    }
}

public class LegacyDbContext : DbContext
{
    private readonly string _conn;
    public LegacyDbContext(string conn) => _conn = conn;

    protected override void OnConfiguring(DbContextOptionsBuilder options)
        => options.UseSqlServer(_conn);
}

public class CurrentDbContext : DbContext
{
    private readonly string _conn;
    public CurrentDbContext(string conn) => _conn = conn;

    protected override void OnConfiguring(DbContextOptionsBuilder options)
        => options.UseSqlServer(_conn);
}
