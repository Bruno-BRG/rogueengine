using RogueEngine.Engine.Commands;
using RogueEngine.Engine.Components;
using RogueEngine.Engine.Core;
using RogueEngine.Engine.Data;
using RogueEngine.Engine.Rules;
using RogueEngine.Engine.Scripting;

namespace RogueEngine.Engine.Tests;

public class GameRulesTests
{
    [Fact]
    public void StatResolver_IncludesEquippedWeaponAttack()
    {
        var rules = CreateRules();
        var player = CreateCombatant(rules, equipWeapon: "rusty_sword");
        var damage = StatResolver.GetAttackDamage(player, rules);
        Assert.Equal(5, damage);
    }

    [Fact]
    public void ItemEffectRegistry_HealEffectWorks()
    {
        var rules = CreateRules();
        var world = new World(new TileMap(5, 5)) { Rules = rules };
        var player = CreateCombatant(rules);
        world.AddEntity(player);
        player.GetComponent<HealthComponent>().TakeDamage(4);

        var potion = rules.Project.Items["health_potion"];
        Assert.True(rules.ItemEffects.Apply(world, player, potion, rules.Project.Items));
        Assert.Equal(10, player.GetComponent<HealthComponent>().CurrentHp);
    }

    [Fact]
    public void InteractionRegistry_DoorRequiresKey()
    {
        var rules = CreateRules();
        var world = new World(new TileMap(5, 5)) { Rules = rules };
        var player = CreateCombatant(rules);
        world.AddEntity(player);

        var door = rules.Project.Interactions["iron_door"];
        var position = new Position(2, 2);
        Assert.False(rules.Interactions.TryInteract(world, player, door, position, rules.Project.Items));

        player.GetComponent<InventoryComponent>().Stacks.Add(new InventoryStack
        {
            ItemId = "iron_key",
            Count = 1
        });

        Assert.True(rules.Interactions.TryInteract(world, player, door, position, rules.Project.Items));
    }

    [Fact]
    public void QuestService_CompletesKillObjectiveAndGrantsReward()
    {
        var rules = CreateRules();
        var world = new World(new TileMap(5, 5)) { Rules = rules };
        var player = CreateCombatant(rules);
        player.AddComponent(new QuestLogComponent());
        world.AddEntity(player);
        rules.Quests.StartQuest(player, "goblin_hunt");

        var goblin = new Entity();
        goblin.AddComponent(new ActorIdComponent("goblin"));
        goblin.AddComponent(new HealthComponent(6));
        world.Raise(new EntityKilledEvent(goblin, player));
        world.Raise(new EntityKilledEvent(goblin, player));
        world.Raise(new EntityKilledEvent(goblin, player));

        var questLog = player.GetComponent<QuestLogComponent>();
        Assert.Contains("goblin_hunt", questLog.CompletedQuestIds);
        Assert.Contains(player.GetComponent<InventoryComponent>().Stacks, stack => stack.ItemId == "health_potion");
    }

    [Fact]
    public void ClassLoader_LoadsStartItems()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"rogue-class-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        try
        {
            var classJson = """
            {
              "id": "warrior",
              "name": "Warrior",
              "baseMaxHp": 25,
              "statBonuses": { "attack": 1 },
              "startItems": [{ "itemId": "rusty_sword", "count": 1 }]
            }
            """;
            File.WriteAllText(Path.Combine(tempDir, "warrior.json"), classJson);
            var classes = ClassLoader.LoadAllFromDirectory(tempDir);
            var warrior = Assert.Single(classes.Values);
            Assert.Equal("warrior", warrior.Id);
            Assert.Single(warrior.StartItems);
        }
        finally
        {
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void GameSaveLoad_RoundTripsQuestLog()
    {
        var world = new World(new TileMap(5, 5));
        var player = CreateCombatant(CreateRules());
        player.AddComponent(new ClassComponent { ClassId = "warrior" });
        var questLog = player.GetComponent<QuestLogComponent>();
        questLog.ActiveQuests.Add(new QuestProgressEntry
        {
            QuestId = "goblin_hunt",
            ObjectiveProgress = [2]
        });
        world.AddEntity(player);

        var savePath = Path.Combine(Path.GetTempPath(), $"rogue-save-{Guid.NewGuid():N}.json");
        try
        {
            GameSaveLoad.Save(world, 42, savePath);
            var loaded = GameSaveLoad.LoadSaveData(savePath);
            Assert.Equal("warrior", loaded.PlayerClassId);
            Assert.NotNull(loaded.QuestLog);
            Assert.Single(loaded.QuestLog.ActiveQuests);
            Assert.Equal("goblin_hunt", loaded.QuestLog.ActiveQuests[0].QuestId);
        }
        finally
        {
            if (File.Exists(savePath)) File.Delete(savePath);
        }
    }

    private static GameRulesContext CreateRules()
    {
        var items = new Dictionary<string, ItemDefinition>(StringComparer.OrdinalIgnoreCase)
        {
            ["health_potion"] = new()
            {
                Id = "health_potion",
                Name = "Health Potion",
                Kind = "consumable",
                OnUse = new ItemUseEffect { Effect = "heal", Amount = 5 }
            },
            ["rusty_sword"] = new()
            {
                Id = "rusty_sword",
                Name = "Rusty Sword",
                Kind = "equipment",
                EquipSlot = "weapon",
                Stats = new ItemStats { Attack = 2 }
            },
            ["iron_key"] = new()
            {
                Id = "iron_key",
                Name = "Iron Key",
                Kind = "key",
                KeyId = "iron"
            }
        };

        var interactions = new Dictionary<string, InteractionDefinition>(StringComparer.OrdinalIgnoreCase)
        {
            ["iron_door"] = new()
            {
                Id = "iron_door",
                Kind = "door",
                RequiredKeyId = "iron"
            }
        };

        var quests = new Dictionary<string, QuestDefinition>(StringComparer.OrdinalIgnoreCase)
        {
            ["goblin_hunt"] = new()
            {
                Id = "goblin_hunt",
                Title = "Goblin Hunt",
                Objectives =
                [
                    new QuestObjectiveDefinition { Type = "kill", ActorId = "goblin", Count = 3 }
                ],
                Rewards =
                [
                    new QuestRewardDefinition { ItemId = "health_potion", Count = 2 }
                ]
            }
        };

        var project = new LoadedProject
        {
            ProjectRoot = ".",
            ReprojPath = "game.reproj",
            Project = new GameProject { StartQuests = ["goblin_hunt"] },
            Settings = new GameSettings(),
            Actors = new Dictionary<string, ActorDefinition>
            {
                ["player"] = new() { Id = "player", MaxHp = 10, IsPlayer = true },
                ["goblin"] = new() { Id = "goblin", MaxHp = 6 }
            },
            Items = items,
            Interactions = interactions,
            Quests = quests
        };

        return new GameRulesContext(project, null);
    }

    private static Entity CreateCombatant(GameRulesContext rules, string? equipWeapon = null)
    {
        var entity = new Entity();
        entity.AddComponent(new ActorIdComponent("player"));
        entity.AddComponent(new PositionComponent(new Position(1, 1)));
        entity.AddComponent(new HealthComponent(10));
        entity.AddComponent(new IsPlayerComponent());
        var inventory = new InventoryComponent();
        if (!string.IsNullOrWhiteSpace(equipWeapon))
        {
            inventory.EquippedWeaponId = equipWeapon;
        }

        entity.AddComponent(inventory);
        entity.AddComponent(new QuestLogComponent());
        return entity;
    }
}
