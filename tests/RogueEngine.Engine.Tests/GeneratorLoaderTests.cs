using RogueEngine.Engine.Data;

namespace RogueEngine.Engine.Tests;

public class GeneratorLoaderTests
{
    [Fact]
    public void Load_ReadsTemplateGenerator()
    {
        var project = ProjectLoader.Load(GetTemplateProjectPath());
        Assert.NotNull(project.Generator);
        Assert.Equal("main_dungeon", project.Generator.Id);
        Assert.Equal("rooms_corridors", project.Generator.Algorithm);
        Assert.Equal(80, project.Generator.Width);
        Assert.Equal(22, project.Generator.Height);
    }

    [Fact]
    public void SaveAndLoad_RoundTripsGeneratorDefinition()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"generator-{Guid.NewGuid():N}.json");
        try
        {
            var original = new GeneratorDefinition
            {
                Id = "test",
                Algorithm = "rooms_corridors",
                Width = 50,
                Height = 30,
                Seed = 42,
                Parameters = new Dictionary<string, object>()
            };

            GeneratorLoader.Save(tempFile, original);
            var loaded = GeneratorLoader.Load(tempFile);

            Assert.Equal(original.Id, loaded.Id);
            Assert.Equal(original.Algorithm, loaded.Algorithm);
            Assert.Equal(original.Width, loaded.Width);
            Assert.Equal(original.Height, loaded.Height);
            Assert.Equal(original.Seed, loaded.Seed);
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
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
