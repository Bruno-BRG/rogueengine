using RogueEngine.BuildTool;

namespace RogueEngine.BuildTool.Tests;

public class RpgDemoBuildTests
{
    [Fact]
    public void Build_Succeeds_ForScriptlessRpgDemoTemplate()
    {
        var reprojPath = LocateRepoFile("templates", "RpgDemoProject", "game.reproj");
        var output = Path.Combine(Path.GetTempPath(), $"rogueengine-rpgdemo-{Guid.NewGuid():N}");

        try
        {
            var result = BuildCommand.Execute(reprojPath, output);

            Assert.True(result.Success, string.Join("; ", result.Errors));
            Assert.True(File.Exists(Path.Combine(output, "RpgDemo.exe")));
            Assert.True(File.Exists(Path.Combine(output, "game.reproj")));
        }
        finally
        {
            if (Directory.Exists(output))
            {
                Directory.Delete(output, recursive: true);
            }
        }
    }

    private static string LocateRepoFile(params string[] parts)
    {
        var current = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(current))
        {
            var candidate = Path.Combine(new[] { current }.Concat(parts).ToArray());
            if (File.Exists(candidate))
            {
                return candidate;
            }

            current = Directory.GetParent(current)?.FullName ?? string.Empty;
        }

        throw new FileNotFoundException($"Could not locate {string.Join('/', parts)}.");
    }
}
