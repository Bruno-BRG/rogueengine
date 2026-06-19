using RogueEngine.Engine.Components;
using RogueEngine.Engine.Core;
using RogueEngine.Engine.TurnBased;

namespace RogueEngine.Engine.Tests;

public class TurnManagerTests
{
    [Fact]
    public void RunEnemyTurns_MovesEnemyTowardPlayer()
    {
        var map = CreateFloorMap(10, 10);
        var world = new World(map);
        var player = CreatePlayer(new Position(5, 5));
        var enemy = CreateEnemy(new Position(2, 5));
        world.AddEntity(player);
        world.AddEntity(enemy);

        var turnManager = new TurnManager();
        turnManager.Register(enemy.GetComponent<EnemyTurnComponent>());

        turnManager.RunEnemyTurns(world);

        var enemyPosition = enemy.GetComponent<PositionComponent>().Position;
        Assert.Equal(new Position(3, 5), enemyPosition);
    }

    [Fact]
    public void RunEnemyTurns_AttacksWhenAdjacent()
    {
        var map = CreateFloorMap(10, 10);
        var world = new World(map);
        var player = CreatePlayer(new Position(5, 5));
        var enemy = CreateEnemy(new Position(4, 5));
        world.AddEntity(player);
        world.AddEntity(enemy);

        var initialHp = player.GetComponent<HealthComponent>().CurrentHp;
        var turnManager = new TurnManager();
        turnManager.Register(enemy.GetComponent<EnemyTurnComponent>());

        turnManager.RunEnemyTurns(world);

        Assert.True(player.GetComponent<HealthComponent>().CurrentHp < initialHp);
        Assert.Contains(world.Log.Messages, message => message.Contains("hits"));
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
        entity.AddComponent(new HealthComponent(20));
        entity.AddComponent(new IsPlayerComponent());
        return entity;
    }

    private static Entity CreateEnemy(Position position)
    {
        var entity = new Entity();
        entity.AddComponent(new PositionComponent(position));
        entity.AddComponent(new RenderableComponent('o', RgbColor.Gray));
        entity.AddComponent(new HealthComponent(6));
        entity.AddComponent(new BlocksMovementComponent());
        entity.AddComponent(new EnemyTurnComponent(entity));
        return entity;
    }
}
