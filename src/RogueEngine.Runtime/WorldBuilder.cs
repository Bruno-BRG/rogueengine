using RogueEngine.Engine.AI;
using RogueEngine.Engine.Components;
using RogueEngine.Engine.Core;
using RogueEngine.Engine.Data;
using RogueEngine.Engine.ProcGen;
using RogueEngine.Engine.Scripting;
using RogueEngine.Engine.TurnBased;
using RogueEngine.Toolkit.Pathfinding;
using RogueEngine.Toolkit.ProcGen;
using RogueEngine.Toolkit.Tiles;

namespace RogueEngine.Runtime;

internal static class WorldBuilder
{
    static WorldBuilder()
    {
        ChaseAI.Navigator = new GridPathfinder();
    }

    public static GameSetup CreateNewGame(LoadedProject project, ScriptAssembly? scripts = null, int? seed = null)
    {
        ArgumentNullException.ThrowIfNull(project);

        var scene = project.DefaultScene;
        var mapSize = ResolveMapSize(project, scene);
        var gameSeed = seed ?? scene?.Seed ?? project.Generator?.Seed ?? Random.Shared.Next();
        var random = new Random(gameSeed);
        var generation = GenerateMap(project, mapSize.Width, mapSize.Height, random);
        var world = new World(generation.Map);
        var turnManager = new TurnManager();

        var playerDefinition = project.Actors.Values.First(actor => actor.IsPlayer);
        var playerPosition = ResolvePlayerSpawn(scene, generation);
        var player = SpawnActor(world, turnManager, scripts, playerDefinition, playerPosition, isPlayer: true);
        world.Log.Add("You enter the dungeon.");

        SpawnSceneEntities(world, turnManager, scripts, project, scene, random);
        SpawnSceneItems(world, project, scene);

        return new GameSetup(world, player, turnManager, gameSeed, project);
    }

    public static GameSetup CreateOverworldGame(LoadedProject project, ScriptAssembly? scripts = null)
    {
        ArgumentNullException.ThrowIfNull(project);
        if (project.DefaultOverworld is null)
        {
            throw new InvalidOperationException("Project does not define a default overworld.");
        }

        var service = new Toolkit.Overworld.OverworldService(project.DefaultOverworld);
        var map = service.BuildOverworldMap();
        var world = new World(map);
        var turnManager = new TurnManager();
        var playerDefinition = project.Actors.Values.First(actor => actor.IsPlayer);
        var player = SpawnActor(
            world,
            turnManager,
            scripts,
            playerDefinition,
            new Position(2, 2),
            isPlayer: true);
        world.Log.Add("You stand on the overworld. Move to a region and press Enter.");
        return new GameSetup(world, player, turnManager, Random.Shared.Next(), project);
    }

    public static GameSetup CreateDungeonFromCell(
        LoadedProject project,
        OverworldCellDefinition cell,
        ScriptAssembly? scripts,
        int seed)
    {
        var service = new Toolkit.Overworld.OverworldService(project.DefaultOverworld!);
        var random = new Random(seed);
        var generation = service.EnterCell(cell, project, random);
        ApplyTileset(project, generation.Map, project.Generator);
        var world = new World(generation.Map);
        var turnManager = new TurnManager();
        var playerDefinition = project.Actors.Values.First(actor => actor.IsPlayer);
        var player = SpawnActor(
            world,
            turnManager,
            scripts,
            playerDefinition,
            generation.Rooms.Count > 0 ? generation.Rooms[0].Center : new Position(2, 2),
            isPlayer: true);
        world.Log.Add($"You enter {cell.Id}.");
        return new GameSetup(world, player, turnManager, seed, project);
    }

    public static GameSetup CreateFromSave(LoadedProject project, SaveData saveData, ScriptAssembly? scripts = null)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(saveData);

        var mapSize = ResolveMapSize(project);
        var random = new Random(saveData.Seed);
        var generation = GenerateMap(project, mapSize.Width, mapSize.Height, random);
        var world = new World(generation.Map);
        var turnManager = new TurnManager();
        Entity? player = null;

        foreach (var snapshot in saveData.Entities)
        {
            if (!string.IsNullOrWhiteSpace(snapshot.PickupItemId))
            {
                SpawnItemPickup(
                    world,
                    project,
                    snapshot.PickupItemId,
                    snapshot.PickupCount > 0 ? snapshot.PickupCount : 1,
                    new Position(snapshot.X, snapshot.Y));
                continue;
            }

            if (string.IsNullOrWhiteSpace(snapshot.ActorId))
            {
                continue;
            }

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
                snapshot.CurrentHp,
                definition.IsPlayer);

            if (snapshot.Inventory is not null)
            {
                RestoreInventory(entity, snapshot.Inventory);
            }

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

    private static void SpawnSceneEntities(
        World world,
        TurnManager turnManager,
        ScriptAssembly? scripts,
        LoadedProject project,
        SceneDefinition? scene,
        Random random)
    {
        if (scene?.Entities.Count > 0)
        {
            foreach (var placement in scene.Entities)
            {
                if (!project.Actors.TryGetValue(placement.ActorId, out var definition) || definition.IsPlayer)
                {
                    continue;
                }

                var position = new Position(placement.X, placement.Y);
                if (world.Map.IsWalkable(position))
                {
                    SpawnActor(world, turnManager, scripts, definition, position);
                }
            }

            return;
        }

        var enemyDefinitions = project.Actors.Values
            .Where(actor => !actor.IsPlayer && actor.HasChaseAI)
            .ToList();

        if (enemyDefinitions.Count == 0)
        {
            return;
        }

        var walkable = new List<Position>();
        for (var x = 0; x < world.Map.Width; x++)
        {
            for (var y = 0; y < world.Map.Height; y++)
            {
                var position = new Position(x, y);
                if (world.Map.IsWalkable(position) && world.GetEntityAt(position) is null)
                {
                    walkable.Add(position);
                }
            }
        }

        var enemyCount = random.Next(project.Settings.MinEnemies, project.Settings.MaxEnemies + 1);
        for (var i = 0; i < enemyCount && walkable.Count > 0; i++)
        {
            var index = random.Next(walkable.Count);
            var position = walkable[index];
            walkable.RemoveAt(index);
            var definition = enemyDefinitions[random.Next(enemyDefinitions.Count)];
            SpawnActor(world, turnManager, scripts, definition, position);
        }
    }

    private static void SpawnSceneItems(World world, LoadedProject project, SceneDefinition? scene)
    {
        if (scene is null)
        {
            return;
        }

        foreach (var placement in scene.ItemPlacements)
        {
            SpawnItemPickup(world, project, placement.ItemId, placement.Count, new Position(placement.X, placement.Y));
        }
    }

    private static void SpawnItemPickup(
        World world,
        LoadedProject project,
        string itemId,
        int count,
        Position position)
    {
        if (!project.Items.TryGetValue(itemId, out var definition) || !world.Map.IsWalkable(position))
        {
            return;
        }

        var entity = new Entity();
        entity.AddComponent(new PositionComponent(position));
        entity.AddComponent(new ItemPickupComponent { ItemId = itemId, Count = count });
        entity.AddComponent(new RenderableComponent(definition.Glyph, definition.Color.ToRgbColor()));
        world.AddEntity(entity);
    }

    private static Entity SpawnActor(
        World world,
        TurnManager turnManager,
        ScriptAssembly? scripts,
        ActorDefinition definition,
        Position position,
        int? currentHp = null,
        bool isPlayer = false)
    {
        var entity = new Entity();
        entity.AddComponent(new ActorIdComponent(definition.Id));
        entity.AddComponent(new PositionComponent(position));
        entity.AddComponent(new RenderableComponent(definition.Glyph, definition.Color.ToRgbColor()));
        entity.AddComponent(new HealthComponent(definition.MaxHp, currentHp));

        if (definition.IsPlayer || isPlayer)
        {
            entity.AddComponent(new IsPlayerComponent());
            entity.AddComponent(new InventoryComponent());
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

    private static void RestoreInventory(Entity entity, InventorySnapshot snapshot)
    {
        if (!entity.TryGetComponent<InventoryComponent>(out var inventory) || inventory is null)
        {
            return;
        }

        inventory.EquippedWeaponId = snapshot.EquippedWeaponId;
        inventory.EquippedArmorId = snapshot.EquippedArmorId;
        inventory.Stacks.Clear();
        foreach (var stack in snapshot.Stacks)
        {
            inventory.Stacks.Add(new InventoryStack { ItemId = stack.ItemId, Count = stack.Count });
        }
    }

    private static (int Width, int Height) ResolveMapSize(LoadedProject project, SceneDefinition? scene = null)
    {
        if (scene?.Width is > 0 && scene.Height is > 0)
        {
            return (scene.Width.Value, scene.Height.Value);
        }

        if (project.Generator is not null)
        {
            return (project.Generator.Width, project.Generator.Height);
        }

        return (project.Settings.MapWidth, project.Settings.MapHeight);
    }

    private static Position ResolvePlayerSpawn(SceneDefinition? scene, DungeonGenerationResult generation)
    {
        if (scene?.PlayerSpawnX is int x && scene.PlayerSpawnY is int y)
        {
            return new Position(x, y);
        }

        return generation.Rooms.Count > 0 ? generation.Rooms[0].Center : new Position(2, 2);
    }

    private static DungeonGenerationResult GenerateMap(LoadedProject project, int width, int height, Random random)
    {
        var definition = project.Generator ?? new GeneratorDefinition
        {
            Id = "main_dungeon",
            Algorithm = "rooms_corridors",
            Width = width,
            Height = height
        };

        var effective = new GeneratorDefinition
        {
            Id = definition.Id,
            Algorithm = definition.Algorithm,
            Width = width,
            Height = height,
            Seed = definition.Seed,
            Parameters = definition.Parameters
        };

        var result = MapGeneratorService.Generate(effective, random);
        ApplyTileset(project, result.Map, effective);
        return result;
    }

    private static void ApplyTileset(LoadedProject project, TileMap map, GeneratorDefinition? definition)
    {
        if (definition?.Parameters is null ||
            !definition.Parameters.TryGetValue("tileset", out var tilesetValue))
        {
            return;
        }

        var tilesetPath = tilesetValue.ToString();
        if (string.IsNullOrWhiteSpace(tilesetPath))
        {
            return;
        }

        var fullPath = Path.Combine(project.DataDirectory, tilesetPath);
        if (!File.Exists(fullPath))
        {
            return;
        }

        var tileSet = TileSetLoader.Load(fullPath);
        AutotileApplicator.Apply(map, tileSet);
    }
}

internal sealed record GameSetup(
    World World,
    Entity Player,
    TurnManager TurnManager,
    int Seed,
    LoadedProject Project);
