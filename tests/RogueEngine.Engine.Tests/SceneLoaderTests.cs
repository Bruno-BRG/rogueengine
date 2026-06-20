using RogueEngine.Engine.Data;

namespace RogueEngine.Engine.Tests;

public class SceneLoaderTests
{
    [Fact]
    public void LoadAllFromDirectory_LoadsTemplateMainScene()
    {
        var repoRoot = FindRepositoryRoot();
        var scenesDirectory = Path.Combine(repoRoot, "templates", "BasicRoguelikeProject", "Data", "scenes");
        var scenes = SceneLoader.LoadAllFromDirectory(scenesDirectory);

        Assert.Single(scenes);
        Assert.Equal("main", scenes[0].Id);
        Assert.Equal("Main Dungeon", scenes[0].Name);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "RogueEngine.slnx"))
                || File.Exists(Path.Combine(directory.FullName, "RogueEngine.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not locate repository root.");
    }
}
