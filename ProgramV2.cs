using Microsoft.SqlServer.Dac;
using Microsoft.SqlServer.Dac.Compare;
using System.Diagnostics;

class Program
{
    static int Main(string[] args)
    {
        if (args.Length != 3)
        {
            Console.WriteLine("Usage:");
            Console.WriteLine("  tool.exe <schema.sql> <connectionString> <output.sql>");
            return 1;
        }

        var sqlFile = args[0];
        var connectionString = args[1];
        var outputFile = args[2];

        try
        {
            var dacpacPath = BuildDacpacFromSql(sqlFile);

            // ==================================================
            // DacFx 170+ Schema Compare (UPDATED API)
            // ==================================================
            var source = SchemaCompareEndpointFactory.CreateDacpacEndpoint(dacpacPath);
            var target = SchemaCompareEndpointFactory.CreateDatabaseEndpoint(connectionString);

            var options = new SchemaCompareOptions
            {
                IgnoreWhitespace = true,
                IgnoreComments = true,
                IgnorePermissions = true,
                IgnoreUserSettingsObjects = true,
                IgnoreRoleMembership = true,

                DropObjectsNotInSource = true,
                BlockOnPossibleDataLoss = false
            };

            var comparison = new SchemaComparison(source, target, options);

            var result = comparison.Compare();

            if (result == null || result.IsValid == false)
            {
                Console.WriteLine("Comparison failed:");

                if (result?.Errors != null)
                {
                    foreach (var err in result.Errors)
                        Console.WriteLine(err.Message);
                }

                return 2;
            }

            var script = result.GenerateScript(
                new SchemaCompareScriptOptions
                {
                    DeployToDatabase = false
                });

            File.WriteAllText(outputFile, script);

            Console.WriteLine($"Diff generated → {outputFile}");
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine(ex);
            return 99;
        }
    }

    // ==================================================
    // SQL → DACPAC (unchanged approach)
    // ==================================================
    static string BuildDacpacFromSql(string sqlFilePath)
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "dacpac_build_" + Guid.NewGuid());
        Directory.CreateDirectory(tempDir);

        var fileName = Path.GetFileName(sqlFilePath);
        var copiedSql = Path.Combine(tempDir, fileName);
        File.Copy(sqlFilePath, copiedSql);

        var projectFile = Path.Combine(tempDir, "schema.sqlproj");
        var dacpacPath = Path.Combine(tempDir, "schema.dacpac");

        var projectContent = $@"
<Project Sdk=""Microsoft.Build.Sql"">
  <PropertyGroup>
    <TargetFramework>netstandard2.0</TargetFramework>
    <SqlServerVersion>Sql150</SqlServerVersion>
  </PropertyGroup>

  <ItemGroup>
    <Build Include=""{fileName}"" />
  </ItemGroup>
</Project>";

        File.WriteAllText(projectFile, projectContent);

        var sqlPackage = "sqlpackage";

        var args =
            $"/Action:Build " +
            $"/SourceFile:\"{projectFile}\" " +
            $"/OutputPath:\"{dacpacPath}\"";

        Run(sqlPackage, args);

        if (!File.Exists(dacpacPath))
            throw new Exception("DACPAC build failed");

        return dacpacPath;
    }

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
                UseShellExecute = false
            }
        };

        p.Start();

        var output = p.StandardOutput.ReadToEnd();
        var error = p.StandardError.ReadToEnd();

        p.WaitForExit();

        Console.WriteLine(output);

        if (p.ExitCode != 0)
            throw new Exception(error);
    }
}
