using RogueEngine.Engine.Core;
using RogueEngine.Engine.Data;

namespace RogueEngine.Engine.Scripting;

public interface IItemEffectContext
{
    World World { get; }
    Entity Entity { get; }
    ItemDefinition Item { get; }
    IReadOnlyDictionary<string, ItemDefinition> Items { get; }
    void Log(string message);
}
