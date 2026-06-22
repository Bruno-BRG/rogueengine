namespace RogueEngine.Engine.Data;

public sealed class SaveData
{
    public int Seed { get; init; }
    public string? PlayerClassId { get; init; }
    public QuestLogSnapshot? QuestLog { get; init; }
    public EntitySnapshot[] Entities { get; init; } = [];
}

public sealed class EntitySnapshot
{
    public string ActorId { get; init; } = string.Empty;
    public int X { get; init; }
    public int Y { get; init; }
    public int CurrentHp { get; init; }
    public string? PickupItemId { get; init; }
    public int PickupCount { get; init; }
    public string? InteractionId { get; init; }
    public bool InteractionConsumed { get; init; }
    public InventorySnapshot? Inventory { get; init; }
}

public sealed class InventorySnapshot
{
    public InventoryStackSnapshot[] Stacks { get; init; } = [];
    public string? EquippedWeaponId { get; init; }
    public string? EquippedArmorId { get; init; }
}

public sealed class InventoryStackSnapshot
{
    public string ItemId { get; init; } = string.Empty;
    public int Count { get; init; }
}

public sealed class QuestLogSnapshot
{
    public QuestProgressSnapshot[] ActiveQuests { get; init; } = [];
    public string[] CompletedQuestIds { get; init; } = [];
}

public sealed class QuestProgressSnapshot
{
    public string QuestId { get; init; } = string.Empty;
    public int[] ObjectiveProgress { get; init; } = [];
}
