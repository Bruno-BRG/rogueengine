using RogueEngine.Engine.Core;

namespace RogueEngine.Engine.Components;

public sealed class ActorIdComponent : IComponent
{
    public string ActorId { get; }

    public ActorIdComponent(string actorId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(actorId);
        ActorId = actorId;
    }
}
