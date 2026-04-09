using System;
using System.IO;
using Microsoft.SqlServer.Dac.Compare;

// -----------------------------
// PATHS
// -----------------------------
var baseDir = Directory.GetCurrentDirectory();

var inputDir = Path.Combine(baseDir, "input");
var outputDir = Path.Combine(baseDir, "output");

Directory.CreateDirectory(outputDir);

var sourcePath = Path.Combine(inputDir, "source.dacpac");
var targetPath = Path.Combine(inputDir, "target.dacpac");

// -----------------------------
// VALIDATION
// -----------------------------
if (!File.Exists(sourcePath))
{
    Console.WriteLine("Missing source.dacpac");
    return;
}

if (!File.Exists(targetPath))
{
    Console.WriteLine("Missing target.dacpac");
    return;
}

// -----------------------------
// LOAD DACPACS
// -----------------------------
Console.WriteLine("Loading DACPACs...");

var source = new SchemaCompareDacpacEndpoint(sourcePath);
var target = new SchemaCompareDacpacEndpoint(targetPath);

// -----------------------------
// OPTIONS
// -----------------------------
var options = new SchemaCompareOptions
{
    IgnoreWhitespace = true,
    IgnoreComments = true,
    IgnoreColumnOrder = true,
    IgnoreObjectPlacementOnSchema = true
};

// -----------------------------
// HELPER
// -----------------------------
void Write(string name, string content)
{
    var path = Path.Combine(outputDir, name);
    File.WriteAllText(path, content ?? string.Empty);
    Console.WriteLine($"Generated: {path}");
}

// -----------------------------
// FORWARD (source → target)
// -----------------------------
Console.WriteLine("Generating forward diff...");

var forward = new SchemaComparison(source, target, options);
forward.Compare();

Write("forward.diff.sql", forward.GenerateScript());

// -----------------------------
// BACKWARD (target → source)
// -----------------------------
Console.WriteLine("Generating backward diff...");

var backward = new SchemaComparison(target, source, options);
backward.Compare();

Write("backward.diff.sql", backward.GenerateScript());

// -----------------------------
Console.WriteLine("Done.");
