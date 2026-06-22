using RogueEngine.Engine.Core;
using RogueEngine.Engine.Data;

namespace RogueEngine.Engine.Scripting;

public interface IQuestObjectiveContext
{
    World World { get; }
    Entity Player { get; }
    QuestDefinition Quest { get; }
    QuestObjectiveDefinition Objective { get; }
    int CurrentProgress { get; }
    void Log(string message);
}
