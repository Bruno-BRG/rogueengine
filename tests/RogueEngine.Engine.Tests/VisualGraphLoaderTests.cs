using RogueEngine.Engine.Data;

namespace RogueEngine.Engine.Tests;

public class VisualGraphLoaderTests
{
    [Fact]
    public void LoadAllFromDirectory_ReadsTemplateVisualScript()
    {
        var project = ProjectLoader.Load(GetTemplateProjectPath());

        Assert.NotEmpty(project.VisualScripts);
        Assert.Contains(project.VisualScripts, graph => graph.Id == "goblin_basic");
    }

    private static string GetTemplateProjectPath()
    {
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

        throw new FileNotFoundException("Template project not found for tests.");
    }
}
