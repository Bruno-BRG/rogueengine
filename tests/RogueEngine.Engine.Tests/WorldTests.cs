using RogueEngine.Engine.Components;
using RogueEngine.Engine.Core;

namespace RogueEngine.Engine.Tests;

public class WorldTests
{
    [Fact]
    public void MoveEntity_Succeeds_WhenTargetIsWalkableAndEmpty()
    {
        var map = CreateFloorMap(5, 5);
        var world = new World(map);
        var player = CreatePlayer(new Position(2, 2));
        world.AddEntity(player);

        var moved = world.MoveEntity(player, new Position(3, 2));

        Assert.True(moved);
        Assert.Equal(new Position(3, 2), player.GetComponent<PositionComponent>().Position);
    }

    [Fact]
    public void MoveEntity_Fails_WhenTargetIsWall()
    {
        var map = CreateFloorMap(5, 5);
        map.SetTile(3, 2, Tile.Wall);
        var world = new World(map);
        var player = CreatePlayer(new Position(2, 2));
        world.AddEntity(player);

        var moved = world.MoveEntity(player, new Position(3, 2));

        Assert.False(moved);
        Assert.Equal(new Position(2, 2), player.GetComponent<PositionComponent>().Position);
    }

    [Fact]
    public void MoveEntity_Fails_WhenBlockedByEntity()
    {
        var map = CreateFloorMap(5, 5);
        var world = new World(map);
        var player = CreatePlayer(new Position(2, 2));
        var blocker = CreateBlockingEntity(new Position(3, 2));
        world.AddEntity(player);
        world.AddEntity(blocker);

        var moved = world.MoveEntity(player, new Position(3, 2));

        Assert.False(moved);
        Assert.Equal(new Position(2, 2), player.GetComponent<PositionComponent>().Position);
    }

    [Fact]
    public void GetEntityAt_ReturnsEntity_AtPosition()
    {
        var map = CreateFloorMap(5, 5);
        var world = new World(map);
        var player = CreatePlayer(new Position(1, 1));
        world.AddEntity(player);

        var found = world.GetEntityAt(new Position(1, 1));

        Assert.Same(player, found);
    }

    private static TileMap CreateFloorMap(int width, int height)
    {
        var map = new TileMap(width, height);
        for (var x = 0; x < width; x++)
        {
            for (var y = 0; y < height; y++)
            {
                map.SetTile(x, y, Tile.Floor);
            }
        }

        return map;
    }

    private static Entity CreatePlayer(Position position)
    {
        var entity = new Entity();
        entity.AddComponent(new PositionComponent(position));
        entity.AddComponent(new RenderableComponent('@', RgbColor.Yellow));
        return entity;
    }

    private static Entity CreateBlockingEntity(Position position)
    {
        var entity = new Entity();
        entity.AddComponent(new PositionComponent(position));
        entity.AddComponent(new RenderableComponent('M', RgbColor.Gray));
        entity.AddComponent(new BlocksMovementComponent());
        return entity;
    }
}
