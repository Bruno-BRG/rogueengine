using RogueEngine.Engine.Core;

namespace RogueEngine.Engine.Components;

public sealed class PositionComponent : IComponent
{
    public Position Position { get; set; }

    public PositionComponent(Position position)
    {
        Position = position;
    }
}
