using RogueEngine.Engine.Core;

namespace RogueEngine.Engine.Components;

public sealed class ClassComponent : IComponent
{
    public required string ClassId { get; init; }
}
