using RogueEngine.BuildTool;

namespace RogueEngine.BuildTool.Tests;

public class AssetCopierTests
{
    [Fact]
    public void CopyProjectAssets_CopiesReprojDataAndScripts()
    {
        var templateRoot = GetTemplateRoot();
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"rogueengine-build-{Guid.NewGuid():N}");

        try
        {
            AssetCopier.CopyProjectAssets(templateRoot, outputDirectory, "Data");

            Assert.True(File.Exists(Path.Combine(outputDirectory, "game.reproj")));
            Assert.True(File.Exists(Path.Combine(outputDirectory, "Data", "settings.json")));
            Assert.True(File.Exists(Path.Combine(outputDirectory, "Data", "actors", "player.json")));
            Assert.True(File.Exists(Path.Combine(outputDirectory, "Scripts", "GoblinBehavior.cs")));
        }
        finally
        {
            if (Directory.Exists(outputDirectory))
            {
                Directory.Delete(outputDirectory, recursive: true);
            }
        }
    }

    private static string GetTemplateRoot()
    {
        var reprojPath = GetTemplateProjectPath();
        return Path.GetDirectoryName(reprojPath)
            ?? throw new InvalidOperationException("Could not resolve template root.");
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
