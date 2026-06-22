using RogueEngine.Engine.Core;
using RogueEngine.Toolkit.ProcGen;

namespace RogueEngine.Toolkit.Tests;

public class MapGeneratorRegistryTests
{
    [Theory]
    [InlineData("rooms_corridors")]
    [InlineData("cellular_caves")]
    [InlineData("drunkard_walk")]
    [InlineData("bsp_dungeon")]
    [InlineData("hybrid_cave_rooms")]
    public void Generate_ProducesWalkableTiles(string algorithm)
    {
        var result = GeneratorRegistry.Generate(algorithm, 40, 25, new Random(42));
        Assert.True(CountWalkable(result.Map) > 0);
    }

    private static int CountWalkable(TileMap map)
    {
        var count = 0;
        for (var x = 0; x < map.Width; x++)
        {
            for (var y = 0; y < map.Height; y++)
            {
                if (map.GetTile(x, y).IsWalkable)
                {
                    count++;
                }
            }
        }

        return count;
    }

    [Fact]
    public void List_IncludesBuiltInAlgorithms()
    {
        var algorithms = GeneratorRegistry.List();
        Assert.Contains("cellular_caves", algorithms);
        Assert.Contains("bsp_dungeon", algorithms);
    }
}
