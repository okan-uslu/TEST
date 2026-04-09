using Microsoft.SqlServer.Dac;
using System.IO;
using System.Data.SqlClient;

class Program
{
    static void Main()
    {
        var baseDir = Directory.GetCurrentDirectory();

        var sourcePath = Path.Combine(baseDir, "inputs", "new.dacpac");
        var outputDir = Path.Combine(baseDir, "outputs");
        var outputPath = Path.Combine(outputDir, "diff.sql");

        Directory.CreateDirectory(outputDir);

        // 🔗 Your target DB connection string
        var connectionString = "Server=.;Database=YourDb;Trusted_Connection=True;";

        var source = DacPackage.Load(sourcePath);

        var dacServices = new DacServices(connectionString);

        // Extract DB name from connection string
        var builder = new SqlConnectionStringBuilder(connectionString);
        var dbName = builder.InitialCatalog;

        var script = dacServices.GenerateDeployScript(
            source,
            dbName,
            new DacDeployOptions()
        );

        File.WriteAllText(outputPath, script);

        System.Console.WriteLine($"Diff script created: {outputPath}");
    }
}
