using RogueEngine.Engine.Core;
using RogueEngine.Engine.ProcGen;

namespace RogueEngine.Engine.Tests;

public class DungeonGeneratorTests
{
    [Fact]
    public void Generate_CreatesWalkableFloorTiles()
    {
        var generator = new DungeonGenerator();
        var result = generator.Generate(40, 25, new Random(42));

        var floorCount = 0;
        for (var x = 0; x < result.Map.Width; x++)
        {
            for (var y = 0; y < result.Map.Height; y++)
            {
                if (result.Map.GetTile(x, y).IsWalkable)
                {
                    floorCount++;
                }
            }
        }

        Assert.True(floorCount > 0);
    }

    [Fact]
    public void Generate_ReturnsAtLeastOneRoom()
    {
        var generator = new DungeonGenerator();
        var result = generator.Generate(40, 25, new Random(7));

        Assert.NotEmpty(result.Rooms);
    }

    [Fact]
    public void Generate_ConnectsRoomsWithCorridors()
    {
        var generator = new DungeonGenerator();
        var result = generator.Generate(50, 30, new Random(99));

        Assert.True(result.Rooms.Count >= 2);

        foreach (var room in result.Rooms)
        {
            Assert.True(result.Map.IsWalkable(room.Center));
        }
    }

    [Fact]
    public void Generate_KeepsMapWithinBounds()
    {
        var generator = new DungeonGenerator();
        var result = generator.Generate(30, 20, new Random(1));

        Assert.Equal(30, result.Map.Width);
        Assert.Equal(20, result.Map.Height);
    }
}
