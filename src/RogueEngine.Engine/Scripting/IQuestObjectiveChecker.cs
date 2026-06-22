namespace RogueEngine.Engine.Scripting;

public interface IQuestObjectiveChecker
{
    bool IsComplete(IQuestObjectiveContext context);
}
