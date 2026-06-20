using RogueEngine.Editor.Models;
using RogueEngine.Engine.VisualScripting;

namespace RogueEngine.Editor.Services;

public sealed class VisualScriptService
{
    public IReadOnlyList<EditorVisualGraph> LoadAll(string projectRoot)
    {
        var directory = Path.Combine(projectRoot, "VisualScripts");
        if (!Directory.Exists(directory))
        {
            return [];
        }

        return Directory.EnumerateFiles(directory, "*.graph.json")
            .Select(path =>
            {
                var graph = VisualGraphLoader.Load(path);
                return EditorVisualGraph.FromEngine(graph, Path.GetFileName(path));
            })
            .OrderBy(graph => graph.Id, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public void Save(EditorVisualGraph graph, string projectRoot)
    {
        var directory = Path.Combine(projectRoot, "VisualScripts");
        Directory.CreateDirectory(directory);

        var fileName = string.IsNullOrWhiteSpace(graph.SourceFileName)
            ? $"{graph.Id}.graph.json"
            : graph.SourceFileName;
        var path = Path.Combine(directory, fileName);
        VisualGraphLoader.Save(path, graph.ToEngine());
        graph.SourceFileName = fileName;
    }

    public GraphGenerationResult GeneratePreview(EditorVisualGraph graph) =>
        GraphCodeGenerator.Generate(graph.ToEngine());

    public IReadOnlyList<string> Validate(EditorVisualGraph graph) =>
        GraphCodeGenerator.Generate(graph.ToEngine()).Errors;
}
