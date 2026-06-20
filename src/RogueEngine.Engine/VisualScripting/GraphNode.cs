namespace RogueEngine.Engine.VisualScripting;

public sealed class GraphNode
{
    public string Id { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public Dictionary<string, object>? Parameters { get; init; }
    public string? Next { get; init; }
    public string? TrueBranch { get; init; }
    public string? FalseBranch { get; init; }
}
