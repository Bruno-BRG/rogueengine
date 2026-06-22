namespace RogueEngine.Runtime;

internal static class ProjectPathResolver
{
    public static string Resolve(string[] args)
    {
        if (args.Length > 0 && !string.IsNullOrWhiteSpace(args[0]))
        {
            return Path.GetFullPath(args[0]);
        }

        var localReproj = ResolveLocalReproj(AppContext.BaseDirectory);
        if (localReproj is not null)
        {
            return localReproj;
        }

        var current = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(current))
        {
            var candidate = Path.Combine(current, "templates", "BasicRoguelikeProject", "game.reproj");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            current = Directory.GetParent(current)?.FullName ?? string.Empty;
        }

        throw new FileNotFoundException(
            "Could not find game.reproj. Place one next to the executable or pass the path as the first argument.");
    }

    private static string? ResolveLocalReproj(string directory)
    {
        if (!Directory.Exists(directory))
        {
            return null;
        }

        var reprojFiles = Directory.GetFiles(directory, "*.reproj", SearchOption.TopDirectoryOnly);
        if (reprojFiles.Length == 1)
        {
            return Path.GetFullPath(reprojFiles[0]);
        }

        var gameReproj = reprojFiles.FirstOrDefault(file =>
            string.Equals(Path.GetFileName(file), "game.reproj", StringComparison.OrdinalIgnoreCase));

        return gameReproj is null ? null : Path.GetFullPath(gameReproj);
    }
}
