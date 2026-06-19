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

        var snapshots = new List<EntitySnapshot>();
        foreach (var entity in world.Entities)
        {
            if (!entity.TryGetComponent<ActorIdComponent>(out var actorId) || actorId is null)
            {
                continue;
            }

            if (!entity.TryGetComponent<PositionComponent>(out var position) || position is null)
            {
                continue;
            }

            if (!entity.TryGetComponent<HealthComponent>(out var health) || health is null)
            {
                continue;
            }

            snapshots.Add(new EntitySnapshot
            {
                ActorId = actorId.ActorId,
                X = position.Position.X,
                Y = position.Position.Y,
                CurrentHp = health.CurrentHp
            });
        }

        var saveData = new SaveData
        {
            Seed = seed,
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
