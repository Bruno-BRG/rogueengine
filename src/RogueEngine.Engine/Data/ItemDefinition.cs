namespace RogueEngine.Engine.Data;

public sealed class ItemDefinition
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public char Glyph { get; init; } = '!';
    public ColorData Color { get; init; } = new();
    public string Kind { get; init; } = "consumable";
    public int MaxStack { get; init; } = 1;
    public string? EquipSlot { get; init; }
    public ItemUseEffect OnUse { get; init; } = new();
}

public sealed class ItemUseEffect
{
    public int Heal { get; init; }
}
