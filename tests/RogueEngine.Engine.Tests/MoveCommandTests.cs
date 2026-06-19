using RogueEngine.Engine.Commands;
using RogueEngine.Engine.Components;
using RogueEngine.Engine.Core;

namespace RogueEngine.Engine.Tests;

public class MoveCommandTests
{
    [Fact]
    public void Execute_MovesEntity_WhenPathIsClear()
    {
        var map = CreateFloorMap(5, 5);
        var world = new World(map);
        var player = CreatePlayer(new Position(2, 2));
        world.AddEntity(player);
        var command = new MoveCommand(player, 1, 0);

        var executed = command.Execute(world);

        Assert.True(executed);
        Assert.Equal(new Position(3, 2), player.GetComponent<PositionComponent>().Position);
    }

    [Fact]
    public void Execute_ReturnsFalse_WhenBlockedByWall()
    {
        var map = CreateFloorMap(5, 5);
        map.SetTile(3, 2, Tile.Wall);
        var world = new World(map);
        var player = CreatePlayer(new Position(2, 2));
        world.AddEntity(player);
        var command = new MoveCommand(player, 1, 0);

        var executed = command.Execute(world);

        Assert.False(executed);
        Assert.Equal(new Position(2, 2), player.GetComponent<PositionComponent>().Position);
    }

    [Fact]
    public void Execute_ReturnsFalse_WhenEntityHasNoPosition()
    {
        var map = CreateFloorMap(5, 5);
        var world = new World(map);
        var entity = new Entity();
        world.AddEntity(entity);
        var command = new MoveCommand(entity, 1, 0);

        var executed = command.Execute(world);

        Assert.False(executed);
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
}
