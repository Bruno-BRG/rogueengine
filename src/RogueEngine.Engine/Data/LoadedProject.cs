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
    public SceneDefinition? DefaultScene { get; init; }
    public OverworldDefinition? DefaultOverworld { get; init; }
    public IReadOnlyDictionary<string, ItemDefinition> Items { get; init; } =
        new Dictionary<string, ItemDefinition>(StringComparer.OrdinalIgnoreCase);
    public IReadOnlyDictionary<string, InteractionDefinition> Interactions { get; init; } =
        new Dictionary<string, InteractionDefinition>(StringComparer.OrdinalIgnoreCase);
    public IReadOnlyDictionary<string, ClassDefinition> Classes { get; init; } =
        new Dictionary<string, ClassDefinition>(StringComparer.OrdinalIgnoreCase);
    public IReadOnlyDictionary<string, QuestDefinition> Quests { get; init; } =
        new Dictionary<string, QuestDefinition>(StringComparer.OrdinalIgnoreCase);
    public IReadOnlyList<VisualGraph> VisualScripts { get; init; } = [];

    public string ItemsDirectory => Path.Combine(DataDirectory, "items");
    public string InteractionsDirectory => Path.Combine(DataDirectory, "interactions");
    public string ClassesDirectory => Path.Combine(DataDirectory, "classes");
    public string QuestsDirectory => Path.Combine(DataDirectory, "quests");

    public string DataDirectory => Path.Combine(ProjectRoot, Project.DataPath);
    public string ScenesDirectory => Path.Combine(DataDirectory, "scenes");
    public string ScriptsDirectory => Path.Combine(ProjectRoot, "Scripts");
    public string VisualScriptsDirectory => Path.Combine(ProjectRoot, "VisualScripts");
    public string SaveFilePath => Path.Combine(ProjectRoot, "save.json");

    public string? GeneratorFilePath =>
        string.IsNullOrWhiteSpace(Project.DefaultGenerator)
            ? null
            : Path.Combine(DataDirectory, Project.DefaultGenerator);
}
