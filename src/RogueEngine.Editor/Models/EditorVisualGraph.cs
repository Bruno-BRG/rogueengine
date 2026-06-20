using RogueEngine.Engine.VisualScripting;

namespace RogueEngine.Editor.Models;

public sealed class EditorVisualGraph
{
    public string SourceFileName { get; set; } = string.Empty;
    public string Id { get; set; } = string.Empty;
    public string EntryNodeId { get; set; } = string.Empty;
    public List<EditorGraphNode> Nodes { get; set; } = [];

    public static EditorVisualGraph FromEngine(VisualGraph graph, string sourceFileName) => new()
    {
        SourceFileName = sourceFileName,
        Id = graph.Id,
        EntryNodeId = graph.EntryNodeId,
        Nodes = graph.Nodes.Select(EditorGraphNode.FromEngine).ToList()
    };

    public VisualGraph ToEngine() => new()
    {
        Id = Id,
        EntryNodeId = EntryNodeId,
        Nodes = Nodes.Select(node => node.ToEngine()).ToList()
    };
}

public sealed class EditorGraphNode
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = NodeType.OnTurn;
    public string? Next { get; set; }
    public string? TrueBranch { get; set; }
    public string? FalseBranch { get; set; }
    public string Message { get; set; } = string.Empty;
    public int Threshold { get; set; } = 1;

    public static EditorGraphNode FromEngine(GraphNode node)
    {
        var editor = new EditorGraphNode
        {
            Id = node.Id,
            Type = node.Type,
            Next = node.Next,
            TrueBranch = node.TrueBranch,
            FalseBranch = node.FalseBranch
        };

        if (node.Parameters is not null)
        {
            if (node.Parameters.TryGetValue("message", out var message))
            {
                editor.Message = message?.ToString() ?? string.Empty;
            }

            if (node.Parameters.TryGetValue("threshold", out var threshold) &&
                int.TryParse(threshold?.ToString(), out var parsed))
            {
                editor.Threshold = parsed;
            }
        }

        return editor;
    }

    public GraphNode ToEngine()
    {
        Dictionary<string, object>? parameters = null;
        if (Type.Equals(NodeType.Log, StringComparison.OrdinalIgnoreCase))
        {
            parameters = new Dictionary<string, object> { ["message"] = Message };
        }
        else if (Type.Equals(NodeType.HasHpBelow, StringComparison.OrdinalIgnoreCase))
        {
            parameters = new Dictionary<string, object> { ["threshold"] = Threshold };
        }

        return new GraphNode
        {
            Id = Id,
            Type = Type,
            Next = string.IsNullOrWhiteSpace(Next) ? null : Next,
            TrueBranch = string.IsNullOrWhiteSpace(TrueBranch) ? null : TrueBranch,
            FalseBranch = string.IsNullOrWhiteSpace(FalseBranch) ? null : FalseBranch,
            Parameters = parameters
        };
    }
}
