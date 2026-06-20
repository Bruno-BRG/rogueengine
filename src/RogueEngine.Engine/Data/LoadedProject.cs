using RogueEngine.Engine.VisualScripting;

namespace RogueEngine.Engine.Data;

public sealed class LoadedProject
{
    public required string ProjectRoot { get; init; }
    public required string ReprojPath { get; init; }
    public required GameProject Project { get; init; }
    public required GameSettings Settings { get; init; }
    public required IReadOnlyDictionary<string, ActorDefinition> Actors { get; init; }
    public GeneratorDefinition? Generator { get; init; }
    public IReadOnlyList<VisualGraph> VisualScripts { get; init; } = [];

    public string DataDirectory => Path.Combine(ProjectRoot, Project.DataPath);
    public string ScriptsDirectory => Path.Combine(ProjectRoot, "Scripts");
    public string VisualScriptsDirectory => Path.Combine(ProjectRoot, "VisualScripts");
    public string SaveFilePath => Path.Combine(ProjectRoot, "save.json");

    public string? GeneratorFilePath =>
        string.IsNullOrWhiteSpace(Project.DefaultGenerator)
            ? null
            : Path.Combine(DataDirectory, Project.DefaultGenerator);
}
