using RogueEngine.Engine.Data;
using RogueEngine.Toolkit.Overworld;

namespace RogueEngine.Toolkit.Tests;

public class OverworldServiceTests
{
    [Fact]
    public void EnterCell_GeneratesLocalMapFromCellGenerator()
    {
        var dataDir = Path.Combine(Path.GetTempPath(), "rogue-overworld", Guid.NewGuid().ToString("N"));
        var projectRoot = dataDir;
        var dataPath = Path.Combine(projectRoot, "Data");
        Directory.CreateDirectory(Path.Combine(dataPath, "generators"));
        Directory.CreateDirectory(Path.Combine(dataPath, "actors"));
        File.WriteAllText(Path.Combine(dataPath, "settings.json"), """{"mapWidth":40,"mapHeight":16,"messagePanelHeight":3,"minEnemies":1,"maxEnemies":2}""");
        File.WriteAllText(Path.Combine(dataPath, "actors", "player.json"), """{"id":"player","glyph":"@","color":{"r":255,"g":255,"b":255},"maxHp":10,"isPlayer":true}""");
        File.WriteAllText(Path.Combine(dataPath, "generators", "caves.json"),
            """
            {
              "id": "caves",
              "algorithm": "cellular_caves",
              "width": 30,
              "height": 12,
              "parameters": { "fillPercent": 45, "smoothPasses": 3, "wallThreshold": 4 }
            }
            """);
        File.WriteAllText(Path.Combine(projectRoot, "game.reproj"),
            """{"name":"test","version":"1","dataPath":"Data","defaultGenerator":"generators/caves.json"}""");

        try
        {
            var project = ProjectLoader.Load(Path.Combine(projectRoot, "game.reproj"));
            var overworld = new OverworldDefinition
            {
                Id = "test",
                Cells =
                [
                    new OverworldCellDefinition
                    {
                        Id = "cave_a",
                        X = 0,
                        Y = 0,
                        Biome = "cave",
                        LocalGenerator = "generators/caves.json"
                    }
                ],
                Connections = []
            };

            var service = new OverworldService(overworld);
            var result = service.EnterCell(overworld.Cells[0], project, new Random(7));
            Assert.Equal(30, result.Map.Width);
            Assert.Equal(12, result.Map.Height);

            var walkable = 0;
            for (var x = 0; x < result.Map.Width; x++)
            {
                for (var y = 0; y < result.Map.Height; y++)
                {
                    if (result.Map.IsWalkable(x, y))
                    {
                        walkable++;
                    }
                }
            }

            Assert.True(walkable > 0);
        }
        finally
        {
            if (Directory.Exists(dataDir))
            {
                Directory.Delete(dataDir, recursive: true);
            }
        }
    }
}
