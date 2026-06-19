using RogueEngine.BuildTool;

if (args.Length == 0 || args[0] is not "build")
{
    PrintUsage();
    return 1;
}

if (args.Length < 2)
{
    BuildLogger.Error("Missing path to game.reproj.");
    PrintUsage();
    return 1;
}

var reprojPath = args[1];
string? outputDirectory = null;

for (var i = 2; i < args.Length; i++)
{
    if (args[i] == "--output" && i + 1 < args.Length)
    {
        outputDirectory = args[++i];
        continue;
    }

    BuildLogger.Error($"Unknown argument: {args[i]}");
    PrintUsage();
    return 1;
}

var result = BuildCommand.Execute(reprojPath, outputDirectory);
return result.Success ? 0 : 1;

static void PrintUsage()
{
    Console.WriteLine("Usage: rogueengine build <path/to/game.reproj> [--output <directory>]");
}
