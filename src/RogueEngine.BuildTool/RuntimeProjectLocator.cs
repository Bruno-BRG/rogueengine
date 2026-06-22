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

    public static bool TryLocateRuntimeTemplate(out string templateDirectory)
    {
        var envPath = Environment.GetEnvironmentVariable("ROGUEENGINE_RUNTIME_TEMPLATE");
        if (!string.IsNullOrWhiteSpace(envPath) && IsValidRuntimeTemplate(envPath))
        {
            templateDirectory = Path.GetFullPath(envPath);
            return true;
        }

        foreach (var root in GetSearchRoots())
        {
            var candidate = Path.Combine(root, "sdk", "runtime-template");
            if (IsValidRuntimeTemplate(candidate))
            {
                templateDirectory = candidate;
                return true;
            }
        }

        templateDirectory = string.Empty;
        return false;
    }

    private static IEnumerable<string> GetSearchRoots()
    {
        var current = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(current))
        {
            yield return current;

            var parent = Directory.GetParent(current)?.FullName;
            if (string.IsNullOrEmpty(parent) || parent == current)
            {
                break;
            }

            current = parent;
        }
    }

    private static bool IsValidRuntimeTemplate(string directory) =>
        Directory.Exists(directory) &&
        File.Exists(Path.Combine(directory, "RogueEngine.Runtime.exe"));
}
