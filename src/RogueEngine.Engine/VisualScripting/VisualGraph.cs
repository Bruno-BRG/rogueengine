namespace RogueEngine.Engine.VisualScripting;

public sealed class VisualGraph
{
    public string Id { get; init; } = string.Empty;
    public string EntryNodeId { get; init; } = string.Empty;
    public IReadOnlyList<GraphNode> Nodes { get; init; } = [];
}
