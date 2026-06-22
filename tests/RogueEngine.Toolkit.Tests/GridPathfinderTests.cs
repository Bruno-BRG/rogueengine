using RogueEngine.Engine.Core;
using RogueEngine.Toolkit.Pathfinding;

namespace RogueEngine.Toolkit.Tests;

public class GridPathfinderTests
{
    [Fact]
    public void FindPath_GoesAroundWall()
    {
        var map = new TileMap(5, 5);
        for (var x = 0; x < map.Width; x++)
        {
            for (var y = 0; y < map.Height; y++)
            {
                map.SetTile(x, y, Tile.Floor);
            }
        }

        map.SetTile(2, 1, Tile.Wall);
        map.SetTile(2, 2, Tile.Wall);
        map.SetTile(2, 3, Tile.Wall);

        var pathfinder = new GridPathfinder();
        var path = pathfinder.FindPath(map, new Position(1, 2), new Position(3, 2));

        Assert.NotNull(path);
        Assert.Equal(new Position(1, 2), path![0]);
        Assert.Equal(new Position(3, 2), path[^1]);
        Assert.DoesNotContain(new Position(2, 2), path);
    }
}
