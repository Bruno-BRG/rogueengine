using RogueEngine.Engine.Core;

namespace RogueEngine.Engine.Components;

public sealed class ItemPickupComponent : IComponent
{
    public required string ItemId { get; init; }
    public int Count { get; init; } = 1;
}
