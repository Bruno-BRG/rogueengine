namespace RogueEngine.Engine.Data;

public sealed class LoadedProject
{
    public required string ProjectRoot { get; init; }
    public required string ReprojPath { get; init; }
    public required GameProject Project { get; init; }
    public required GameSettings Settings { get; init; }
    public required IReadOnlyDictionary<string, ActorDefinition> Actors { get; init; }

    public string DataDirectory => Path.Combine(ProjectRoot, Project.DataPath);
    public string ScriptsDirectory => Path.Combine(ProjectRoot, "Scripts");
    public string SaveFilePath => Path.Combine(ProjectRoot, "save.json");
}
