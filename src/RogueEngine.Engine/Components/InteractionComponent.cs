using RogueEngine.Engine.Core;

namespace RogueEngine.Engine.Components;

public sealed class InteractionComponent : IComponent
{
    public required string InteractionId { get; init; }
    public bool IsConsumed { get; set; }
}
