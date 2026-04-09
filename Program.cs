using System;
using System.IO;
using Microsoft.SqlServer.Dac;

var currentDir = Directory.GetCurrentDirectory();

var sourcePath = Path.Combine(currentDir, "source.dacpac");
var targetPath = Path.Combine(currentDir, "target.dacpac");

if (!File.Exists(sourcePath))
{
    Console.WriteLine("source.dacpac not found in current directory.");
    return;
}

if (!File.Exists(targetPath))
{
    Console.WriteLine("target.dacpac not found in current directory.");
    return;
}

var outputDir = Path.Combine(currentDir, "output");
Directory.CreateDirectory(outputDir);

Console.WriteLine("Loading DACPACs...");

var sourcePackage = DacPackage.Load(sourcePath);
var targetPackage = DacPackage.Load(targetPath);

var options = new DacDeployOptions
{
    BlockOnPossibleDataLoss = false,
    DropObjectsNotInSource = false,
    IgnorePermissions = true,
    IgnoreUserSettingsObjects = true
};

Console.WriteLine("Generating forward diff (source → target)...");

var forwardScript = DacServices.GenerateDeployScript(
    sourcePackage,
    targetPackage,
    options
);

File.WriteAllText(
    Path.Combine(outputDir, "forward.diff.sql"),
    forwardScript
);

Console.WriteLine("Generating backward diff (target → source)...");

var backwardScript = DacServices.GenerateDeployScript(
    targetPackage,
    sourcePackage,
    options
);

File.WriteAllText(
    Path.Combine(outputDir, "backward.diff.sql"),
    backwardScript
);

Console.WriteLine("Done.");
