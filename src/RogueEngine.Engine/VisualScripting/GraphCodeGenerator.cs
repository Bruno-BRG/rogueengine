using System.Text;
using System.Text.Json;
using RogueEngine.Engine.Core;

namespace RogueEngine.Engine.VisualScripting;

public static class GraphCodeGenerator
{
    public static GraphGenerationResult Generate(VisualGraph graph)
    {
        ArgumentNullException.ThrowIfNull(graph);

        var errors = Validate(graph);
        if (errors.Count > 0)
        {
            return new GraphGenerationResult { Errors = errors };
        }

        var nodesById = graph.Nodes.ToDictionary(node => node.Id, StringComparer.Ordinal);
        var entry = nodesById[graph.EntryNodeId];
        var className = SanitizeIdentifier(graph.Id);
        var builder = new StringBuilder();

        builder.AppendLine("using RogueEngine.Engine.Core;");
        builder.AppendLine("using RogueEngine.Engine.Scripting;");
        builder.AppendLine();
        builder.AppendLine($"public sealed class {className} : IBehavior");
        builder.AppendLine("{");
        builder.AppendLine("    public void OnTurn(IScriptContext context)");
        builder.AppendLine("    {");

        if (!string.IsNullOrWhiteSpace(entry.Next))
        {
            builder.AppendLine($"        {MethodName(entry.Next)}(context);");
        }

        builder.AppendLine("    }");

        foreach (var node in graph.Nodes)
        {
            if (NodeType.IsEvent(node.Type))
            {
                continue;
            }

            AppendNodeMethod(builder, node, nodesById);
        }

        builder.AppendLine();
        builder.AppendLine("    private static bool IsPlayerAdjacent(IScriptContext context)");
        builder.AppendLine("    {");
        builder.AppendLine("        var player = context.FindPlayer();");
        builder.AppendLine("        if (player is null) return false;");
        builder.AppendLine("        var deltaX = Math.Abs(context.Position.X - player.Value.X);");
        builder.AppendLine("        var deltaY = Math.Abs(context.Position.Y - player.Value.Y);");
        builder.AppendLine("        return deltaX <= 1 && deltaY <= 1 && (deltaX != 0 || deltaY != 0);");
        builder.AppendLine("    }");
        builder.AppendLine("}");

        return new GraphGenerationResult { Source = builder.ToString() };
    }

    private static void AppendNodeMethod(StringBuilder builder, GraphNode node, IReadOnlyDictionary<string, GraphNode> nodesById)
    {
        builder.AppendLine();
        builder.AppendLine($"    private static void {MethodName(node.Id)}(IScriptContext context)");
        builder.AppendLine("    {");

        if (NodeType.IsCondition(node.Type))
        {
            var condition = BuildConditionExpression(node);
            builder.AppendLine($"        if ({condition})");
            builder.AppendLine("        {");
            AppendBranchCall(builder, node.TrueBranch);
            builder.AppendLine("        }");
            builder.AppendLine("        else");
            builder.AppendLine("        {");
            AppendBranchCall(builder, node.FalseBranch);
            builder.AppendLine("        }");
        }
        else
        {
            AppendActionStatements(builder, node);
            AppendBranchCall(builder, node.Next);
        }

        builder.AppendLine("    }");
    }

    private static void AppendActionStatements(StringBuilder builder, GraphNode node)
    {
        switch (node.Type)
        {
            case NodeType.MoveTowardPlayer:
                builder.AppendLine("        var _player = context.FindPlayer();");
                builder.AppendLine("        if (_player.HasValue) context.MoveToward(_player.Value);");
                break;
            case NodeType.AttackAtPlayer:
                builder.AppendLine("        var _player = context.FindPlayer();");
                builder.AppendLine("        if (_player.HasValue) context.AttackAt(_player.Value);");
                break;
            case NodeType.Log:
                var message = GetStringParameter(node, "message") ?? "Visual script log message.";
                builder.AppendLine($"        context.Log({EscapeString(message)});");
                break;
            default:
                builder.AppendLine($"        // Unsupported action: {node.Type}");
                break;
        }
    }

    private static string BuildConditionExpression(GraphNode node)
    {
        if (node.Type.Equals(NodeType.IsPlayerAdjacent, StringComparison.OrdinalIgnoreCase))
        {
            return "IsPlayerAdjacent(context)";
        }

        if (node.Type.Equals(NodeType.HasHpBelow, StringComparison.OrdinalIgnoreCase))
        {
            var threshold = GetIntParameter(node, "threshold") ?? 1;
            return $"context.CurrentHp < {threshold}";
        }

        return "false";
    }

    private static void AppendBranchCall(StringBuilder builder, string? nodeId)
    {
        if (string.IsNullOrWhiteSpace(nodeId))
        {
            return;
        }

        builder.AppendLine($"            {MethodName(nodeId)}(context);");
    }

    private static List<string> Validate(VisualGraph graph)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(graph.Id))
        {
            errors.Add("Visual graph id is required.");
        }

        if (string.IsNullOrWhiteSpace(graph.EntryNodeId))
        {
            errors.Add("Visual graph entryNodeId is required.");
            return errors;
        }

        if (graph.Nodes.Count == 0)
        {
            errors.Add("Visual graph must contain at least one node.");
            return errors;
        }

        var nodesById = new Dictionary<string, GraphNode>(StringComparer.Ordinal);
        foreach (var node in graph.Nodes)
        {
            if (string.IsNullOrWhiteSpace(node.Id))
            {
                errors.Add("Visual graph node id is required.");
                continue;
            }

            if (!nodesById.TryAdd(node.Id, node))
            {
                errors.Add($"Duplicate node id '{node.Id}'.");
            }

            if (!NodeType.All.Contains(node.Type))
            {
                errors.Add($"Unknown node type '{node.Type}' on node '{node.Id}'.");
            }
        }

        if (!nodesById.TryGetValue(graph.EntryNodeId, out var entry))
        {
            errors.Add($"Entry node '{graph.EntryNodeId}' was not found.");
            return errors;
        }

        if (!NodeType.IsEvent(entry.Type))
        {
            errors.Add($"Entry node '{graph.EntryNodeId}' must be type '{NodeType.OnTurn}'.");
        }

        foreach (var node in graph.Nodes)
        {
            ValidateNodeReferences(node, nodesById, errors);
        }

        if (errors.Count == 0 && HasCycle(graph, nodesById))
        {
            errors.Add("Visual graph contains a cycle.");
        }

        return errors;
    }

    private static void ValidateNodeReferences(
        GraphNode node,
        IReadOnlyDictionary<string, GraphNode> nodesById,
        List<string> errors)
    {
        ValidateReference(node, node.Next, nodesById, errors);
        ValidateReference(node, node.TrueBranch, nodesById, errors);
        ValidateReference(node, node.FalseBranch, nodesById, errors);

        if (NodeType.IsCondition(node.Type))
        {
            if (node.Type.Equals(NodeType.HasHpBelow, StringComparison.OrdinalIgnoreCase) &&
                GetIntParameter(node, "threshold") is null)
            {
                errors.Add($"Node '{node.Id}' requires integer parameter 'threshold'.");
            }
        }

        if (node.Type.Equals(NodeType.Log, StringComparison.OrdinalIgnoreCase) &&
            string.IsNullOrWhiteSpace(GetStringParameter(node, "message")))
        {
            errors.Add($"Node '{node.Id}' requires string parameter 'message'.");
        }
    }

    private static void ValidateReference(
        GraphNode node,
        string? targetId,
        IReadOnlyDictionary<string, GraphNode> nodesById,
        List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(targetId))
        {
            return;
        }

        if (!nodesById.ContainsKey(targetId))
        {
            errors.Add($"Node '{node.Id}' references missing node '{targetId}'.");
        }
    }

    private static bool HasCycle(VisualGraph graph, IReadOnlyDictionary<string, GraphNode> nodesById)
    {
        var visiting = new HashSet<string>(StringComparer.Ordinal);
        var visited = new HashSet<string>(StringComparer.Ordinal);

        bool Dfs(string nodeId)
        {
            if (visited.Contains(nodeId))
            {
                return false;
            }

            if (!visiting.Add(nodeId))
            {
                return true;
            }

            if (!nodesById.TryGetValue(nodeId, out var node))
            {
                visiting.Remove(nodeId);
                return false;
            }

            foreach (var nextId in GetOutgoing(node))
            {
                if (Dfs(nextId))
                {
                    return true;
                }
            }

            visiting.Remove(nodeId);
            visited.Add(nodeId);
            return false;
        }

        return Dfs(graph.EntryNodeId);
    }

    private static IEnumerable<string> GetOutgoing(GraphNode node)
    {
        if (!string.IsNullOrWhiteSpace(node.Next))
        {
            yield return node.Next;
        }

        if (!string.IsNullOrWhiteSpace(node.TrueBranch))
        {
            yield return node.TrueBranch;
        }

        if (!string.IsNullOrWhiteSpace(node.FalseBranch))
        {
            yield return node.FalseBranch;
        }
    }

    private static string MethodName(string nodeId) => $"Node_{SanitizeIdentifier(nodeId)}";

    private static string SanitizeIdentifier(string value)
    {
        var builder = new StringBuilder();
        foreach (var ch in value)
        {
            builder.Append(char.IsLetterOrDigit(ch) ? ch : '_');
        }

        var result = builder.ToString();
        if (result.Length == 0 || char.IsDigit(result[0]))
        {
            result = $"N_{result}";
        }

        return result;
    }

    private static string? GetStringParameter(GraphNode node, string key)
    {
        if (node.Parameters is null || !node.Parameters.TryGetValue(key, out var value))
        {
            return null;
        }

        return value switch
        {
            string text => text,
            JsonElement element when element.ValueKind == JsonValueKind.String => element.GetString(),
            _ => value?.ToString()
        };
    }

    private static int? GetIntParameter(GraphNode node, string key)
    {
        if (node.Parameters is null || !node.Parameters.TryGetValue(key, out var value))
        {
            return null;
        }

        return value switch
        {
            int number => number,
            long number => (int)number,
            JsonElement element when element.ValueKind == JsonValueKind.Number && element.TryGetInt32(out var number) => number,
            JsonElement element when element.ValueKind == JsonValueKind.String && int.TryParse(element.GetString(), out var parsed) => parsed,
            _ when int.TryParse(value?.ToString(), out var parsed) => parsed,
            _ => null
        };
    }

    private static string EscapeString(string value) =>
        "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
}
