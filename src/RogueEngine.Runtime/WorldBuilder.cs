using RogueEngine.Engine.Components;
using RogueEngine.Engine.Core;
using RogueEngine.Engine.Data;
using RogueEngine.Engine.Scripting;
using RogueEngine.Engine.ProcGen;
using RogueEngine.Engine.TurnBased;

namespace RogueEngine.Runtime;

internal static class WorldBuilder
{
    public static GameSetup CreateNewGame(LoadedProject project, ScriptAssembly? scripts = null, int? seed = null)
    {
        ArgumentNullException.ThrowIfNull(project);

        var gameSeed = seed ?? Random.Shared.Next();
        var random = new Random(gameSeed);
        var generator = new DungeonGenerator();
        var generation = generator.Generate(project.Settings.MapWidth, project.Settings.MapHeight, random);
        var world = new World(generation.Map);
        var turnManager = new TurnManager();

        var playerDefinition = project.Actors.Values.First(actor => actor.IsPlayer);
        var playerRoom = generation.Rooms[0];
        var player = SpawnActor(world, turnManager, scripts, playerDefinition, playerRoom.Center);
        world.Log.Add("You enter the dungeon.");

        var enemyDefinitions = project.Actors.Values
            .Where(actor => !actor.IsPlayer && actor.HasChaseAI)
            .ToList();

        if (enemyDefinitions.Count > 0)
        {
            var enemyRooms = generation.Rooms.Skip(1).ToList();
            var enemyCount = random.Next(project.Settings.MinEnemies, project.Settings.MaxEnemies + 1);

            for (var i = 0; i < enemyCount && enemyRooms.Count > 0; i++)
            {
                var roomIndex = random.Next(enemyRooms.Count);
                var room = enemyRooms[roomIndex];
                enemyRooms.RemoveAt(roomIndex);

                var definition = enemyDefinitions[random.Next(enemyDefinitions.Count)];
                SpawnActor(world, turnManager, scripts, definition, room.Center);
            }
        }

        return new GameSetup(world, player, turnManager, gameSeed, project);
    }

    public static GameSetup CreateFromSave(LoadedProject project, SaveData saveData, ScriptAssembly? scripts = null)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(saveData);

        var random = new Random(saveData.Seed);
        var generator = new DungeonGenerator();
        var generation = generator.Generate(project.Settings.MapWidth, project.Settings.MapHeight, random);
        var world = new World(generation.Map);
        var turnManager = new TurnManager();
        Entity? player = null;

        foreach (var snapshot in saveData.Entities)
        {
            if (!project.Actors.TryGetValue(snapshot.ActorId, out var definition))
            {
                throw new InvalidDataException($"Unknown actor id in save file: {snapshot.ActorId}");
            }

            var entity = SpawnActor(
                world,
                turnManager,
                scripts,
                definition,
                new Position(snapshot.X, snapshot.Y),
                snapshot.CurrentHp);

            if (definition.IsPlayer)
            {
                player = entity;
            }
        }

        if (player is null)
        {
            throw new InvalidDataException("Save file does not contain a player entity.");
        }

        world.Log.Add("Game loaded.");
        return new GameSetup(world, player, turnManager, saveData.Seed, project);
    }

    private static Entity SpawnActor(
        World world,
        TurnManager turnManager,
        ScriptAssembly? scripts,
        ActorDefinition definition,
        Position position,
        int? currentHp = null)
    {
        var entity = new Entity();
        entity.AddComponent(new ActorIdComponent(definition.Id));
        entity.AddComponent(new PositionComponent(position));
        entity.AddComponent(new RenderableComponent(definition.Glyph, definition.Color.ToRgbColor()));
        entity.AddComponent(new HealthComponent(definition.MaxHp, currentHp));

        if (definition.IsPlayer)
        {
            entity.AddComponent(new IsPlayerComponent());
        }

        if (definition.BlocksMovement)
        {
            entity.AddComponent(new BlocksMovementComponent());
        }

        if (!string.IsNullOrWhiteSpace(definition.Behavior))
        {
            if (scripts is null)
            {
                throw new InvalidOperationException(
                    $"Actor '{definition.Id}' requires behavior '{definition.Behavior}', but no scripts were compiled.");
            }

            var behavior = scripts.CreateBehavior(definition.Behavior);
            if (behavior is null)
            {
                throw new InvalidOperationException(
                    $"Behavior '{definition.Behavior}' was not found in compiled scripts.");
            }

            var behaviorTurn = new BehaviorComponent(entity, behavior);
            entity.AddComponent(behaviorTurn);
            turnManager.Register(behaviorTurn);
        }
        else if (definition.HasChaseAI)
        {
            var enemyTurn = new EnemyTurnComponent(entity);
            entity.AddComponent(enemyTurn);
            turnManager.Register(enemyTurn);
        }

        world.AddEntity(entity);
        return entity;
    }
}

internal sealed record GameSetup(
    World World,
    Entity Player,
    TurnManager TurnManager,
    int Seed,
    LoadedProject Project);
