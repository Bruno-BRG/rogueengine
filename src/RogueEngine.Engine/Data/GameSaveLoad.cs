using System.Text.Json;
using RogueEngine.Engine.Components;
using RogueEngine.Engine.Core;

namespace RogueEngine.Engine.Data;

public static class GameSaveLoad
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    public static void Save(World world, int seed, string savePath)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentException.ThrowIfNullOrWhiteSpace(savePath);

        string? playerClassId = null;
        QuestLogSnapshot? questLogSnapshot = null;
        var player = world.GetPlayer();
        if (player is not null)
        {
            if (player.TryGetComponent<ClassComponent>(out var classComponent) && classComponent is not null)
            {
                playerClassId = classComponent.ClassId;
            }

            if (player.TryGetComponent<QuestLogComponent>(out var questLog) && questLog is not null)
            {
                questLogSnapshot = new QuestLogSnapshot
                {
                    CompletedQuestIds = questLog.CompletedQuestIds.ToArray(),
                    ActiveQuests = questLog.ActiveQuests
                        .Select(entry => new QuestProgressSnapshot
                        {
                            QuestId = entry.QuestId,
                            ObjectiveProgress = entry.ObjectiveProgress.ToArray()
                        })
                        .ToArray()
                };
            }
        }

        var snapshots = new List<EntitySnapshot>();
        foreach (var entity in world.Entities)
        {
            if (!entity.TryGetComponent<PositionComponent>(out var position) || position is null)
            {
                continue;
            }

            if (entity.TryGetComponent<ItemPickupComponent>(out var pickup) && pickup is not null)
            {
                snapshots.Add(new EntitySnapshot
                {
                    PickupItemId = pickup.ItemId,
                    PickupCount = pickup.Count,
                    X = position.Position.X,
                    Y = position.Position.Y
                });
                continue;
            }

            if (entity.TryGetComponent<InteractionComponent>(out var interaction) && interaction is not null)
            {
                snapshots.Add(new EntitySnapshot
                {
                    InteractionId = interaction.InteractionId,
                    InteractionConsumed = interaction.IsConsumed,
                    X = position.Position.X,
                    Y = position.Position.Y
                });
                continue;
            }

            if (!entity.TryGetComponent<ActorIdComponent>(out var actorId) || actorId is null)
            {
                continue;
            }

            if (!entity.TryGetComponent<HealthComponent>(out var health) || health is null)
            {
                continue;
            }

            InventorySnapshot? inventorySnapshot = null;
            if (entity.TryGetComponent<InventoryComponent>(out var inventory) && inventory is not null)
            {
                inventorySnapshot = new InventorySnapshot
                {
                    EquippedWeaponId = inventory.EquippedWeaponId,
                    EquippedArmorId = inventory.EquippedArmorId,
                    Stacks = inventory.Stacks
                        .Select(stack => new InventoryStackSnapshot
                        {
                            ItemId = stack.ItemId,
                            Count = stack.Count
                        })
                        .ToArray()
                };
            }

            snapshots.Add(new EntitySnapshot
            {
                ActorId = actorId.ActorId,
                X = position.Position.X,
                Y = position.Position.Y,
                CurrentHp = health.CurrentHp,
                Inventory = inventorySnapshot
            });
        }

        var saveData = new SaveData
        {
            Seed = seed,
            PlayerClassId = playerClassId,
            QuestLog = questLogSnapshot,
            Entities = snapshots.ToArray()
        };

        var directory = Path.GetDirectoryName(savePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(saveData, JsonOptions);
        File.WriteAllText(savePath, json);
    }

    public static SaveData LoadSaveData(string savePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(savePath);

        if (!File.Exists(savePath))
        {
            throw new FileNotFoundException($"Save file not found: {savePath}");
        }

        var json = File.ReadAllText(savePath);
        return JsonSerializer.Deserialize<SaveData>(json, JsonOptions)
            ?? throw new InvalidDataException($"Failed to deserialize save file: {savePath}");
    }

    public static bool TryLoadSaveData(string savePath, out SaveData? saveData)
    {
        saveData = null;
        if (!File.Exists(savePath))
        {
            return false;
        }

        saveData = LoadSaveData(savePath);
        return true;
    }
}
