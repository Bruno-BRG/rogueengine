using RogueEngine.BuildTool;
using RogueEngine.Engine.Data;

namespace RogueEngine.BuildTool.Tests;

public class VisualScriptBuildTests
{
    [Fact]
    public void CompileProjectScripts_CompilesTemplateVisualScript()
    {
        var reprojPath = GetTemplateProjectPath();
        var project = ProjectLoader.Load(reprojPath);
        var errors = new List<string>();

        var result = ScriptBuildHelper.CompileProjectScripts(project, errors);

        Assert.Empty(errors);
        Assert.True(result.Success, string.Join("; ", result.Errors));
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
