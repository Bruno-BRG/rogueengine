using RogueEngine.Engine.Core;

namespace RogueEngine.Engine.Components;

public sealed class InventoryComponent : IComponent
{
    public List<InventoryStack> Stacks { get; } = [];
    public string? EquippedWeaponId { get; set; }
    public string? EquippedArmorId { get; set; }
}

public sealed class InventoryStack
{
    public required string ItemId { get; init; }
    public int Count { get; set; }
}
