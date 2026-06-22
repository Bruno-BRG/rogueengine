using RogueEngine.Engine.Core;
using RogueEngine.Engine.Data;

namespace RogueEngine.Engine.Scripting;

public interface IInteractionContext
{
    World World { get; }
    Entity Entity { get; }
    InteractionDefinition Interaction { get; }
    Position Position { get; }
    IReadOnlyDictionary<string, ItemDefinition> Items { get; }
    void Log(string message);
}
