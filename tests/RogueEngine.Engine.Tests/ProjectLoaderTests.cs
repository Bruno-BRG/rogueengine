using RogueEngine.Engine.Data;

namespace RogueEngine.Engine.Tests;

public class ProjectLoaderTests
{
    [Fact]
    public void Load_ReadsTemplateProject()
    {
        var reprojPath = GetTemplateProjectPath();
        var project = ProjectLoader.Load(reprojPath);

        Assert.Equal("BasicRoguelike", project.Project.Name);
        Assert.Equal(80, project.Settings.MapWidth);
        Assert.Equal(22, project.Settings.MapHeight);
    }

    [Fact]
    public void Load_ReadsPlayerAndGoblinActors()
    {
        var project = ProjectLoader.Load(GetTemplateProjectPath());

        Assert.True(project.Actors.ContainsKey("player"));
        Assert.True(project.Actors.ContainsKey("goblin"));

        var player = project.Actors["player"];
        Assert.True(player.IsPlayer);
        Assert.Equal('@', player.Glyph);
        Assert.Equal(20, player.MaxHp);

        var goblin = project.Actors["goblin"];
        Assert.False(goblin.IsPlayer);
        Assert.Equal("GoblinBehavior", goblin.Behavior);
        Assert.Equal(6, goblin.MaxHp);
    }

    [Fact]
    public void Load_ThrowsWhenProjectFileMissing()
    {
        Assert.Throws<FileNotFoundException>(() => ProjectLoader.Load("missing/game.reproj"));
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
