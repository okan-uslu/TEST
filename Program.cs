using System;
using System.IO;
using Microsoft.SqlServer.Dac.Compare;

// ----------------------------------------------------
// ARGUMENTS
// ----------------------------------------------------
if (args.Length != 2)
{
    Console.WriteLine("Usage:");
    Console.WriteLine("  tool.exe <source.dacpac> <target-connection-string>");
    return;
}

var sourceDacpacPath = args[0];
var targetConnectionString = args[1];

// ----------------------------------------------------
// VALIDATION
// ----------------------------------------------------
if (!File.Exists(sourceDacpacPath))
{
    Console.WriteLine($"Source DACPAC not found: {sourceDacpacPath}");
    return;
}

if (string.IsNullOrWhiteSpace(targetConnectionString))
{
    Console.WriteLine("Target connection string is empty.");
    return;
}

// ----------------------------------------------------
// OUTPUT DIR
// ----------------------------------------------------
var outputDir = Path.Combine(Directory.GetCurrentDirectory(), "output");
Directory.CreateDirectory(outputDir);

// ----------------------------------------------------
// ENDPOINTS
// ----------------------------------------------------
Console.WriteLine("Loading source DACPAC...");

var source = new SchemaCompareDacpacEndpoint(sourceDacpacPath);

Console.WriteLine("Connecting to target database...");

var target = new SchemaCompareDatabaseEndpoint(targetConnectionString);

// ----------------------------------------------------
// OPTIONS
// ----------------------------------------------------
var options = new SchemaCompareOptions
{
    IgnoreWhitespace = true,
    IgnoreComments = true,
    IgnoreColumnOrder = true,
    IgnoreObjectPlacementOnSchema = true
};

// ----------------------------------------------------
// HELPER
// ----------------------------------------------------
void Write(string fileName, string content)
{
    var path = Path.Combine(outputDir, fileName);
    File.WriteAllText(path, content ?? string.Empty);
    Console.WriteLine($"Generated: {path}");
}

// ----------------------------------------------------
// FORWARD DIFF
// ----------------------------------------------------
Console.WriteLine("Generating forward diff...");

var forward = new SchemaComparison(source, target, options);
forward.Compare();

Write("forward.diff.sql", forward.GenerateScript());

// ----------------------------------------------------
// BACKWARD DIFF
// ----------------------------------------------------
Console.WriteLine("Generating backward diff...");

var backward = new SchemaComparison(target, source, options);
backward.Compare();

Write("backward.diff.sql", backward.GenerateScript());

// ----------------------------------------------------
Console.WriteLine("Done.");
