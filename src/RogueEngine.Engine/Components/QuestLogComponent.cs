using RogueEngine.Engine.Core;

namespace RogueEngine.Engine.Components;

public sealed class QuestLogComponent : IComponent
{
    public List<QuestProgressEntry> ActiveQuests { get; } = [];
    public HashSet<string> CompletedQuestIds { get; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class QuestProgressEntry
{
    public required string QuestId { get; init; }
    public List<int> ObjectiveProgress { get; set; } = [];
}
