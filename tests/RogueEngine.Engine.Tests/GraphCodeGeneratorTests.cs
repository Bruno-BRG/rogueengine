using RogueEngine.Engine.Scripting;
using RogueEngine.Engine.VisualScripting;

namespace RogueEngine.Engine.Tests;

public class GraphCodeGeneratorTests
{
    [Fact]
    public void Generate_ProducesCompilableSource_ForGoblinGraph()
    {
        var graph = CreateGoblinGraph();
        var generation = GraphCodeGenerator.Generate(graph);

        Assert.True(generation.Success, string.Join("; ", generation.Errors));
        var compileResult = ScriptCompiler.Compile([WriteTempSource(generation.Source)]);
        Assert.True(compileResult.Success, string.Join("; ", compileResult.Errors));
    }

    [Fact]
    public void Generate_ReturnsError_ForUnknownNodeType()
    {
        var graph = new VisualGraph
        {
            Id = "bad_graph",
            EntryNodeId = "n1",
            Nodes =
            [
                new GraphNode { Id = "n1", Type = NodeType.OnTurn, Next = "n2" },
                new GraphNode { Id = "n2", Type = "SpawnEnemy", Next = null }
            ]
        };

        var generation = GraphCodeGenerator.Generate(graph);

        Assert.False(generation.Success);
        Assert.Contains(generation.Errors, error => error.Contains("Unknown node type"));
    }

    [Fact]
    public void Generate_ReturnsError_ForCycle()
    {
        var graph = new VisualGraph
        {
            Id = "cycle_graph",
            EntryNodeId = "n1",
            Nodes =
            [
                new GraphNode { Id = "n1", Type = NodeType.OnTurn, Next = "n2" },
                new GraphNode { Id = "n2", Type = NodeType.Log, Next = "n2", Parameters = new Dictionary<string, object> { ["message"] = "loop" } }
            ]
        };

        var generation = GraphCodeGenerator.Generate(graph);

        Assert.False(generation.Success);
        Assert.Contains(generation.Errors, error => error.Contains("cycle", StringComparison.OrdinalIgnoreCase));
    }

    private static VisualGraph CreateGoblinGraph() => new()
    {
        Id = "goblin_basic",
        EntryNodeId = "n1",
        Nodes =
        [
            new GraphNode { Id = "n1", Type = NodeType.OnTurn, Next = "n2" },
            new GraphNode { Id = "n2", Type = NodeType.IsPlayerAdjacent, TrueBranch = "n3", FalseBranch = "n4" },
            new GraphNode { Id = "n3", Type = NodeType.AttackAtPlayer, Next = null },
            new GraphNode { Id = "n4", Type = NodeType.MoveTowardPlayer, Next = null }
        ]
    };

    private static string WriteTempSource(string source)
    {
        var path = Path.Combine(Path.GetTempPath(), $"rogueengine-test-{Guid.NewGuid():N}.generated.cs");
        File.WriteAllText(path, source);
        return path;
    }
}
