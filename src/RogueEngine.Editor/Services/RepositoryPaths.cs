namespace RogueEngine.Editor.Services;

public static class RepositoryPaths
{
    public static string LocateTemplateProjectRoot()
    {
        var current = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(current))
        {
            var candidate = Path.Combine(current, "templates", "BasicRoguelikeProject");
            if (Directory.Exists(candidate) && File.Exists(Path.Combine(candidate, "game.reproj")))
            {
                return candidate;
            }

            current = Directory.GetParent(current)?.FullName ?? string.Empty;
        }

        throw new FileNotFoundException("Could not locate templates/BasicRoguelikeProject.");
    }
}
