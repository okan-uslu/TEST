using System;
using System.IO;
using Microsoft.SqlServer.Dac.Compare;

// -----------------------------
// Base directories
// -----------------------------
var baseDir = Directory.GetCurrentDirectory();

var inputDir = Path.Combine(baseDir, "input");
var outputDir = Path.Combine(baseDir, "output");

// -----------------------------
// Input files
// -----------------------------
var sourcePath = Path.Combine(inputDir, "source.dacpac");
var targetPath = Path.Combine(inputDir, "target.dacpac");

// -----------------------------
// Validate input
// -----------------------------
if (!File.Exists(sourcePath))
{
    Console.WriteLine($"Missing file: {sourcePath}");
    return;
}

if (!File.Exists(targetPath))
{
    Console.WriteLine($"Missing file: {targetPath}");
    return;
}

Directory.CreateDirectory(outputDir);

// -----------------------------
// DACFx endpoints
// -----------------------------
var source = new SchemaCompareDacpacEndpoint(sourcePath);
var target = new SchemaCompareDacpacEndpoint(targetPath);

// -----------------------------
// Options
// -----------------------------
var options = new SchemaCompareOptions
{
    IgnoreWhitespace = true,
    IgnoreComments = true,
    IgnoreColumnOrder = true,
    IgnoreObjectPlacementOnSchema = true
};

// -----------------------------
// Helper
// -----------------------------
static void Write(string path, string content)
{
    File.WriteAllText(path, content ?? string.Empty);
    Console.WriteLine($"Generated: {path}");
}

// -----------------------------
// FORWARD DIFF (source → target)
// -----------------------------
Console.WriteLine("Generating forward diff...");

var forward = new SchemaComparison(source, target, options);
forward.Compare();

var forwardSql = forward.GenerateScript();

var forwardPath = Path.Combine(outputDir, "forward.diff.sql");
Write(forwardPath, forwardSql);

// -----------------------------
// BACKWARD DIFF (target → source)
// -----------------------------
Console.WriteLine("Generating backward diff...");

var backward = new SchemaComparison(target, source, options);
backward.Compare();

var backwardSql = backward.GenerateScript();

var backwardPath = Path.Combine(outputDir, "backward.diff.sql");
Write(backwardPath, backwardSql);

// -----------------------------
// DONE
// -----------------------------
Console.WriteLine("Done.");
