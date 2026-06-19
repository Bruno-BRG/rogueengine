using RogueEngine.Engine.Components;
using RogueEngine.Engine.Core;
using RogueEngine.Engine.Scripting;
using RogueEngine.Engine.TurnBased;

namespace RogueEngine.Engine.Tests;

public class BehaviorComponentTests
{
    [Fact]
    public void TakeTurn_ExecutesCompiledBehavior()
    {
        var scriptPath = WriteTempScript(
            """
            using RogueEngine.Engine.Core;
            using RogueEngine.Engine.Scripting;

            public sealed class StepRightBehavior : IBehavior
            {
                public void OnTurn(IScriptContext context)
                {
                    context.MoveToward(context.Position + new Position(1, 0));
                }
            }
            """);

        try
        {
            var compileResult = ScriptCompiler.Compile([scriptPath]);
            Assert.True(compileResult.Success);

            var scripts = new ScriptAssembly(compileResult.Assembly!);
            var behavior = scripts.CreateBehavior("StepRightBehavior");
            Assert.NotNull(behavior);

            var map = CreateFloorMap(10, 10);
            var world = new World(map);
            var enemy = CreateEnemy(new Position(2, 5));
            var behaviorComponent = new BehaviorComponent(enemy, behavior!);
            enemy.AddComponent(behaviorComponent);
            world.AddEntity(enemy);

            var turnManager = new TurnManager();
            turnManager.Register(behaviorComponent);
            turnManager.RunEnemyTurns(world);

            Assert.Equal(new Position(3, 5), enemy.GetComponent<PositionComponent>().Position);
        }
        finally
        {
            File.Delete(scriptPath);
        }
    }

    private static string WriteTempScript(string source)
    {
        var path = Path.Combine(Path.GetTempPath(), $"rogueengine-script-{Guid.NewGuid():N}.cs");
        File.WriteAllText(path, source);
        return path;
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

    private static Entity CreateEnemy(Position position)
    {
        var entity = new Entity();
        entity.AddComponent(new ActorIdComponent("goblin"));
        entity.AddComponent(new PositionComponent(position));
        entity.AddComponent(new RenderableComponent('o', RgbColor.Gray));
        entity.AddComponent(new HealthComponent(6));
        entity.AddComponent(new BlocksMovementComponent());
        return entity;
    }
}
