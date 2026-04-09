using System;
using System.IO;
using Microsoft.SqlServer.Dac;

var sourceConnection = "Server=.;Database=SourceDb;Trusted_Connection=True;TrustServerCertificate=True;";
var targetConnection = "Server=.;Database=TargetDb;Trusted_Connection=True;TrustServerCertificate=True;";

var outputDir = Path.Combine(Directory.GetCurrentDirectory(), "output");
Directory.CreateDirectory(outputDir);

Console.WriteLine("Initializing DAC services...");

var services = new DacServices(sourceConnection);

var options = new DacDeployOptions
{
    BlockOnPossibleDataLoss = false,
    DropObjectsNotInSource = false,
    IgnorePermissions = true,
    IgnoreUserSettingsObjects = true
};

Console.WriteLine("Generating forward diff (Source DB → Target DB)...");

var forwardScript = services.GenerateDeployScript(
    sourceConnection,
    targetConnection,
    options
);

File.WriteAllText(
    Path.Combine(outputDir, "forward.diff.sql"),
    forwardScript
);

Console.WriteLine("Generating backward diff (Target DB → Source DB)...");

var backwardScript = services.GenerateDeployScript(
    targetConnection,
    sourceConnection,
    options
);

File.WriteAllText(
    Path.Combine(outputDir, "backward.diff.sql"),
    backwardScript
);

Console.WriteLine("Done.");
