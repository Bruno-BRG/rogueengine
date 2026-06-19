using RogueEngine.BuildTool;

namespace RogueEngine.BuildTool.Tests;

public class ProjectValidatorTests
{
    [Fact]
    public void Validate_Succeeds_ForTemplateProject()
    {
        var result = ProjectValidator.Validate(GetTemplateProjectPath());

        Assert.True(result.Success);
        Assert.NotNull(result.Project);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_Fails_WhenProjectFileMissing()
    {
        var result = ProjectValidator.Validate("missing/game.reproj");

        Assert.False(result.Success);
        Assert.NotEmpty(result.Errors);
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
