using Microsoft.SqlServer.Dac;
using Microsoft.SqlServer.Dac.Compare;

class Program
{
    static int Main(string[] args)
    {
        if (args.Length != 3)
        {
            Console.WriteLine("Usage:");
            Console.WriteLine("  tool.exe <source.dacpac> <target-connection-string> <output.sql>");
            return 1;
        }

        var sourceDacpac = args[0];
        var connectionString = args[1];
        var outputFile = args[2];

        try
        {
            // --------------------------------------------------
            // 1. Define source & target
            // --------------------------------------------------
            var source = new SchemaCompareDacpacEndpoint(sourceDacpac);
            var target = new SchemaCompareDatabaseEndpoint(connectionString);

            // --------------------------------------------------
            // 2. Configure comparison options (industry defaults)
            // --------------------------------------------------
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

            var comparison = new SchemaCompare(source, target)
            {
                Options = options
            };

            // --------------------------------------------------
            // 3. Run comparison
            // --------------------------------------------------
            var result = comparison.Compare();

            if (!result.IsValid)
            {
                Console.WriteLine("Comparison failed.");
                foreach (var err in result.Errors)
                    Console.WriteLine(err.Message);

                return 2;
            }

            // --------------------------------------------------
            // 4. Generate SQL diff script
            // --------------------------------------------------
            var script = result.GenerateScript();

            File.WriteAllText(outputFile, script);

            Console.WriteLine($"Diff generated successfully → {outputFile}");

            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine(ex.Message);
            return 99;
        }
    }
}
