namespace RogueEngine.BuildTool;

public static class RuntimeProjectLocator
{
    public static string LocateRuntimeCsproj()
    {
        var envPath = Environment.GetEnvironmentVariable("ROGUEENGINE_RUNTIME_PATH");
        if (!string.IsNullOrWhiteSpace(envPath) && File.Exists(envPath))
        {
            return Path.GetFullPath(envPath);
        }

        var current = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(current))
        {
            var candidate = Path.Combine(current, "src", "RogueEngine.Runtime", "RogueEngine.Runtime.csproj");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            current = Directory.GetParent(current)?.FullName ?? string.Empty;
        }

        throw new FileNotFoundException(
            "Could not locate RogueEngine.Runtime.csproj. Set ROGUEENGINE_RUNTIME_PATH to the csproj path.");
    }
}
