using Microsoft.SqlServer.Dac;
using System;
using System.IO;
using System.Data.SqlClient;

class Program
{
    static void Main()
    {
        // ======================
        // PATHS
        // ======================
        var baseDir = Directory.GetCurrentDirectory();

        var dacpacPath = Path.Combine(baseDir, "inputs", "source.dacpac");
        var outputDir = Path.Combine(baseDir, "outputs");
        var outputPath = Path.Combine(outputDir, "diff.sql");

        Directory.CreateDirectory(outputDir);

        // ======================
        // LOAD DACPAC
        // ======================
        var source = DacPackage.Load(dacpacPath);

        // ======================
        // TARGET DB CONNECTION
        // ======================
        var connectionString =
            "Server=.;Database=YourDb;Trusted_Connection=True;";

        var builder = new SqlConnectionStringBuilder(connectionString);
        var databaseName = builder.InitialCatalog;

        var dacServices = new DacServices(connectionString);

        // ======================
        // OPTIONS (clean + safe)
        // ======================
        var options = new DacDeployOptions
        {
            BlockOnPossibleDataLoss = true,
            DropObjectsNotInSource = false,

            IgnorePermissions = true,
            IgnoreUserSettingsObjects = true,
            IgnoreRoleMembership = true,
            IgnoreExtendedProperties = true
        };

        // ======================
        // GENERATE DIFF SQL
        // ======================
        Console.WriteLine("Generating diff script...");

        var script = dacServices.GenerateDeployScript(
            source,
            databaseName,
            options
        );

        // ======================
        // SAVE OUTPUT
        // ======================
        File.WriteAllText(outputPath, script);

        Console.WriteLine($"Done: {outputPath}");
    }
}
