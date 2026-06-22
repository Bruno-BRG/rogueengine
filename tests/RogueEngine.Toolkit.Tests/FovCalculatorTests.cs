using RogueEngine.Engine.Core;
using RogueEngine.Toolkit.Fov;

namespace RogueEngine.Toolkit.Tests;

public class FovCalculatorTests
{
    [Fact]
    public void Compute_WallBlocksLineOfSight()
    {
        var map = new TileMap(7, 7);
        for (var x = 0; x < map.Width; x++)
        {
            for (var y = 0; y < map.Height; y++)
            {
                map.SetTile(x, y, Tile.Floor);
            }
        }

        for (var y = 0; y < map.Height; y++)
        {
            map.SetTile(3, y, Tile.Wall);
        }

        var visible = FovCalculator.Compute(map, new Position(1, 3), radius: 6);

        Assert.True(visible[2, 3]);
        Assert.False(visible[5, 3]);
    }
}
