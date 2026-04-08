using Microsoft.Data.SqlClient;
using System.Diagnostics;

class Program
{
    static string targetConnectionString;
    static string sqlFilePath;
    static string migrationPath;

    static string masterConn =
        "Server=(localdb)\\MSSQLLocalDB;Database=master;Trusted_Connection=True;";

    static string tempDbName = "EfReconcilerTempDb";

    static string tempConn =>
        $"Server=(localdb)\\MSSQLLocalDB;Database={tempDbName};Trusted_Connection=True;";

    static void Main(string[] args)
    {
        if (args.Length < 3)
        {
            Console.WriteLine("""
Usage:
  EfSchemaReconciler --target "<conn>" --sql "<file.sql>" --migrations "<folder>"
""");
            return;
        }

        targetConnectionString = GetArg(args, "--target");
        sqlFilePath = GetArg(args, "--sql");
        migrationPath = GetArg(args, "--migrations");

        Console.WriteLine("▶ Creating temp DB...");
        CreateTempDb();

        Console.WriteLine("▶ Applying legacy schema...");
        ApplyLegacySchema(sqlFilePath);

        Console.WriteLine("▶ Scaffolding legacy model...");
        ScaffoldLegacyModel();

        Console.WriteLine("▶ Scaffolding current model...");
        ScaffoldCurrentModel();

        Console.WriteLine("▶ Generating migration...");
        GenerateMigration(migrationPath);

        Console.WriteLine("▶ Cleaning up temp DB...");
        DropTempDb();

        Console.WriteLine("✔ DONE");
    }

    // ----------------------------
    // ARG PARSER
    // ----------------------------
    static string GetArg(string[] args, string key)
    {
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == key && i + 1 < args.Length)
                return args[i + 1];
        }

        throw new Exception($"Missing argument: {key}");
    }

    // ----------------------------
    // TEMP DB
    // ----------------------------
    static void CreateTempDb()
    {
        using var conn = new SqlConnection(masterConn);
        conn.Open();

        new SqlCommand($"IF DB_ID('{tempDbName}') IS NOT NULL DROP DATABASE {tempDbName}", conn)
            .ExecuteNonQuery();

        new SqlCommand($"CREATE DATABASE {tempDbName}", conn)
            .ExecuteNonQuery();
    }

    static void DropTempDb()
    {
        using var conn = new SqlConnection(masterConn);
        conn.Open();

        new SqlCommand(
            $"ALTER DATABASE {tempDbName} SET SINGLE_USER WITH ROLLBACK IMMEDIATE",
            conn).ExecuteNonQuery();

        new SqlCommand($"DROP DATABASE {tempDbName}", conn)
            .ExecuteNonQuery();
    }

    // ----------------------------
    // LEGACY SQL EXECUTION
    // ----------------------------
    static void ApplyLegacySchema(string path)
    {
        var sql = File.ReadAllText(path);

        using var conn = new SqlConnection(tempConn);
        conn.Open();

        var batches = sql.Split("GO", StringSplitOptions.RemoveEmptyEntries);

        foreach (var batch in batches)
        {
            if (!string.IsNullOrWhiteSpace(batch))
                new SqlCommand(batch, conn).ExecuteNonQuery();
        }
    }

    // ----------------------------
    // EF SCAFFOLDING
    // ----------------------------
    static void ScaffoldLegacyModel()
    {
        Run("dotnet",
            $"ef dbcontext scaffold \"{tempConn}\" Microsoft.EntityFrameworkCore.SqlServer " +
            $"--context LegacyDbContext --output-dir Models/Legacy --force");
    }

    static void ScaffoldCurrentModel()
    {
        Run("dotnet",
            $"ef dbcontext scaffold \"{targetConnectionString}\" Microsoft.EntityFrameworkCore.SqlServer " +
            $"--context CurrentDbContext --output-dir Models/Current --force");
    }

    // ----------------------------
    // MIGRATION OUTPUT PATH
    // ----------------------------
    static void GenerateMigration(string outputDir)
    {
        Directory.CreateDirectory(outputDir);

        Run("dotnet",
            $"ef migrations add SchemaReconciliation " +
            $"--context CurrentDbContext " +
            $"--output-dir \"{outputDir}\"");
    }

    // ----------------------------
    // PROCESS HELPER
    // ----------------------------
    static void Run(string file, string args)
    {
        var p = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = file,
                Arguments = args,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        p.Start();
        p.WaitForExit();

        if (p.ExitCode != 0)
            throw new Exception(p.StandardError.ReadToEnd());
    }
}
