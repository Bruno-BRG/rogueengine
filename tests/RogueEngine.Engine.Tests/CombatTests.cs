using RogueEngine.Engine.Commands;
using RogueEngine.Engine.Components;
using RogueEngine.Engine.Combat;
using RogueEngine.Engine.Core;

namespace RogueEngine.Engine.Tests;

public class CombatTests
{
    [Fact]
    public void AttackCommand_ReducesTargetHealth()
    {
        var map = CreateFloorMap(5, 5);
        var world = new World(map);
        var attacker = CreateCombatant(new Position(1, 1), '@', 20);
        var target = CreateCombatant(new Position(2, 1), 'o', 10);
        world.AddEntity(attacker);
        world.AddEntity(target);

        var executed = new AttackCommand(attacker, target, 4).Execute(world);

        Assert.True(executed);
        Assert.Equal(6, target.GetComponent<HealthComponent>().CurrentHp);
    }

    [Fact]
    public void AttackCommand_RemovesTargetWhenHealthReachesZero()
    {
        var map = CreateFloorMap(5, 5);
        var world = new World(map);
        var attacker = CreateCombatant(new Position(1, 1), '@', 20);
        var target = CreateCombatant(new Position(2, 1), 'o', 3);
        world.AddEntity(attacker);
        world.AddEntity(target);

        var executed = new AttackCommand(attacker, target, 3).Execute(world);

        Assert.True(executed);
        Assert.DoesNotContain(target, world.Entities);
        Assert.Contains(world.Log.Messages, message => message.Contains("dies"));
    }

    [Fact]
    public void MoveCommand_BumpAttacksBlockingEntityWithHealth()
    {
        var map = CreateFloorMap(5, 5);
        var world = new World(map);
        var player = CreatePlayer(new Position(2, 2));
        var enemy = CreateEnemy(new Position(3, 2));
        world.AddEntity(player);
        world.AddEntity(enemy);

        var initialHp = enemy.GetComponent<HealthComponent>().CurrentHp;
        var executed = new MoveCommand(player, 1, 0).Execute(world);

        Assert.True(executed);
        Assert.Equal(player.GetComponent<PositionComponent>().Position, new Position(2, 2));
        Assert.True(enemy.GetComponent<HealthComponent>().CurrentHp < initialHp);
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

    private static Entity CreateCombatant(Position position, char glyph, int hp)
    {
        var entity = new Entity();
        entity.AddComponent(new PositionComponent(position));
        entity.AddComponent(new RenderableComponent(glyph, RgbColor.White));
        entity.AddComponent(new HealthComponent(hp));
        return entity;
    }

    private static Entity CreatePlayer(Position position)
    {
        var entity = CreateCombatant(position, '@', 20);
        entity.AddComponent(new IsPlayerComponent());
        return entity;
    }

    private static Entity CreateEnemy(Position position)
    {
        var entity = CreateCombatant(position, 'o', 6);
        entity.AddComponent(new BlocksMovementComponent());
        return entity;
    }
}
