using RogueEngine.Engine.Data;

namespace RogueEngine.Runtime;

internal static class ProjectPathResolver
{
    public static string Resolve(string[] args)
    {
        if (args.Length > 0)
        {
            return Path.GetFullPath(args[0]);
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
            "Could not find default template project. Pass a path to game.reproj as the first argument.");
    }
}
