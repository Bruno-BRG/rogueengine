namespace RogueEngine.Engine.Data;

public sealed class QuestObjectiveDefinition
{
    public string Type { get; init; } = string.Empty;
    public string? ActorId { get; init; }
    public string? ItemId { get; init; }
    public int Count { get; init; } = 1;
    public int? X { get; init; }
    public int? Y { get; init; }
    public string? Message { get; init; }
    public string? Script { get; init; }
}

public sealed class QuestRewardDefinition
{
    public string ItemId { get; init; } = string.Empty;
    public int Count { get; init; } = 1;
}

public sealed class QuestCompletionScript
{
    public string? Script { get; init; }
}

public sealed class QuestDefinition
{
    public string Id { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public IReadOnlyList<QuestObjectiveDefinition> Objectives { get; init; } = [];
    public IReadOnlyList<QuestRewardDefinition> Rewards { get; init; } = [];
    public QuestCompletionScript? OnComplete { get; init; }
}
