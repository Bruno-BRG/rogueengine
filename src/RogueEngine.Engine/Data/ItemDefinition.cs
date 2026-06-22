namespace RogueEngine.Engine.Data;

public sealed class ItemStats
{
    public int Attack { get; init; }
    public int Defense { get; init; }
}

public sealed class ItemDefinition
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public char Glyph { get; init; } = '!';
    public ColorData Color { get; init; } = new();
    public string Kind { get; init; } = "consumable";
    public int MaxStack { get; init; } = 1;
    public string? EquipSlot { get; init; }
    public string? KeyId { get; init; }
    public ItemStats Stats { get; init; } = new();
    public ItemUseEffect OnUse { get; init; } = new();
}

public sealed class ItemUseEffect
{
    public string? Effect { get; init; }
    public int Amount { get; init; }
    public int Heal { get; init; }
    public string? Script { get; init; }
}
