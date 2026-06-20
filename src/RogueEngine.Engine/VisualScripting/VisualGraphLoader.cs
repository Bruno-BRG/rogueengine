using System.Text.Json;
using System.Text.Json.Serialization;
using RogueEngine.Engine.Data;

namespace RogueEngine.Engine.VisualScripting;

public static class VisualGraphLoader
{
    private static readonly JsonSerializerOptions ReadOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    private static readonly JsonSerializerOptions WriteOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true
    };

    public static VisualGraph Load(string path)
    {
        var graph = ProjectJson.DeserializeFile<VisualGraph>(path);
        if (string.IsNullOrWhiteSpace(graph.Id))
        {
            throw new InvalidDataException($"Visual graph is missing id: {path}");
        }

        return graph;
    }

    public static void Save(string path, VisualGraph graph) =>
        ProjectJson.SerializeFile(path, graph);

    public static IReadOnlyList<VisualGraph> LoadAllFromDirectory(string directory)
    {
        if (!Directory.Exists(directory))
        {
            return [];
        }

        var graphs = new List<VisualGraph>();
        foreach (var file in Directory.EnumerateFiles(directory, "*.graph.json"))
        {
            graphs.Add(Load(file));
        }

        return graphs;
    }
}
