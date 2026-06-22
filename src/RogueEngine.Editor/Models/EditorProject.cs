using RogueEngine.Engine.Data;
using RogueEngine.Engine.VisualScripting;

namespace RogueEngine.Editor.Models;

public sealed class EditorProject
{
    public string ReprojPath { get; set; } = string.Empty;
    public string ProjectRoot { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = "1";
    public string DataPath { get; set; } = "Data";
    public string DefaultGeneratorPath { get; set; } = "generators/dungeon.json";
    public string? DefaultScenePath { get; set; }
    public string? DefaultOverworldPath { get; set; }
    public EditorSettings Settings { get; set; } = new();
    public List<EditorActor> Actors { get; set; } = [];
    public List<EditorItem> Items { get; set; } = [];
    public EditorGenerator Generator { get; set; } = new();
    public EditorOverworld? Overworld { get; set; }
    public List<EditorVisualGraph> VisualGraphs { get; set; } = [];
    public List<EditorScene> Scenes { get; set; } = [];
    public List<EditorScriptFile> ScriptFiles { get; set; } = [];
    public bool IsDirty { get; set; }

    public string DataDirectory => Path.Combine(ProjectRoot, DataPath);
    public string ActorsDirectory => Path.Combine(DataDirectory, "actors");
    public string ItemsDirectory => Path.Combine(DataDirectory, "items");
    public string ScenesDirectory => Path.Combine(DataDirectory, "scenes");
    public string OverworldDirectory => Path.Combine(DataDirectory, "overworld");
    public string GeneratorFilePath => Path.Combine(DataDirectory, DefaultGeneratorPath);
    public string VisualScriptsDirectory => Path.Combine(ProjectRoot, "VisualScripts");
    public string ScriptsDirectory => Path.Combine(ProjectRoot, "Scripts");

    public static EditorProject FromLoaded(LoadedProject loaded)
    {
        var editor = new EditorProject
        {
            ReprojPath = loaded.ReprojPath,
            ProjectRoot = loaded.ProjectRoot,
            Name = loaded.Project.Name,
            Version = loaded.Project.Version,
            DataPath = loaded.Project.DataPath,
            DefaultGeneratorPath = loaded.Project.DefaultGenerator ?? "generators/dungeon.json",
            DefaultScenePath = loaded.Project.DefaultScene,
            DefaultOverworldPath = loaded.Project.DefaultOverworld,
            Settings = EditorSettings.FromEngine(loaded.Settings),
            Actors = loaded.Actors.Values
                .Select(EditorActor.FromEngine)
                .OrderBy(actor => actor.Id, StringComparer.OrdinalIgnoreCase)
                .ToList(),
            Items = loaded.Items.Values
                .Select(EditorItem.FromEngine)
                .OrderBy(item => item.Id, StringComparer.OrdinalIgnoreCase)
                .ToList(),
            Generator = loaded.Generator is not null
                ? EditorGenerator.FromEngine(loaded.Generator)
                : new EditorGenerator
                {
                    Width = loaded.Settings.MapWidth,
                    Height = loaded.Settings.MapHeight
                }
        };

        if (Directory.Exists(editor.ScenesDirectory))
        {
            editor.Scenes = Directory
                .EnumerateFiles(editor.ScenesDirectory, "*.scene.json")
                .Select(path =>
                {
                    var scene = SceneLoader.Load(path);
                    return EditorScene.FromEngine(scene, Path.GetFileName(path));
                })
                .OrderBy(scene => scene.Id, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        if (Directory.Exists(editor.ScriptsDirectory))
        {
            editor.ScriptFiles = Directory
                .EnumerateFiles(editor.ScriptsDirectory, "*.cs")
                .Select(path => new EditorScriptFile
                {
                    FileName = Path.GetFileName(path),
                    FullPath = path,
                    Content = File.ReadAllText(path)
                })
                .OrderBy(script => script.FileName, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        if (loaded.DefaultOverworld is not null)
        {
            editor.Overworld = EditorOverworld.FromEngine(
                loaded.DefaultOverworld,
                Path.GetFileName(loaded.Project.DefaultOverworld ?? "world.json"));
        }

        return editor;
    }

    public GameProject ToGameProject() => new()
    {
        Name = Name,
        Version = Version,
        DataPath = DataPath,
        DefaultGenerator = DefaultGeneratorPath,
        DefaultScene = DefaultScenePath,
        DefaultOverworld = DefaultOverworldPath
    };

    public GameSettings ToGameSettings() => Settings.ToEngine();

    public GeneratorDefinition ToGeneratorDefinition() => Generator.ToEngine();
}

public sealed class EditorSettings
{
    public int MapWidth { get; set; } = 80;
    public int MapHeight { get; set; } = 22;
    public int MessagePanelHeight { get; set; } = 3;
    public int MinEnemies { get; set; } = 3;
    public int MaxEnemies { get; set; } = 5;

    public static EditorSettings FromEngine(GameSettings settings) => new()
    {
        MapWidth = settings.MapWidth,
        MapHeight = settings.MapHeight,
        MessagePanelHeight = settings.MessagePanelHeight,
        MinEnemies = settings.MinEnemies,
        MaxEnemies = settings.MaxEnemies
    };

    public GameSettings ToEngine() => new()
    {
        MapWidth = MapWidth,
        MapHeight = MapHeight,
        MessagePanelHeight = MessagePanelHeight,
        MinEnemies = MinEnemies,
        MaxEnemies = MaxEnemies
    };
}

public sealed class EditorActor
{
    public string Id { get; set; } = string.Empty;
    public string SourceFileName { get; set; } = string.Empty;
    public char Glyph { get; set; } = '?';
    public byte ColorR { get; set; }
    public byte ColorG { get; set; }
    public byte ColorB { get; set; }
    public int MaxHp { get; set; } = 1;
    public bool IsPlayer { get; set; }
    public bool BlocksMovement { get; set; }
    public bool HasChaseAI { get; set; }
    public string? Behavior { get; set; }

    public static EditorActor FromEngine(ActorDefinition actor) => new()
    {
        Id = actor.Id,
        SourceFileName = $"{actor.Id}.json",
        Glyph = actor.Glyph,
        ColorR = actor.Color.R,
        ColorG = actor.Color.G,
        ColorB = actor.Color.B,
        MaxHp = actor.MaxHp,
        IsPlayer = actor.IsPlayer,
        BlocksMovement = actor.BlocksMovement,
        HasChaseAI = actor.HasChaseAI,
        Behavior = actor.Behavior
    };

    public ActorDefinition ToEngine() => new()
    {
        Id = Id,
        Glyph = Glyph,
        Color = new ColorData { R = ColorR, G = ColorG, B = ColorB },
        MaxHp = MaxHp,
        IsPlayer = IsPlayer,
        BlocksMovement = BlocksMovement,
        HasChaseAI = HasChaseAI,
        Behavior = string.IsNullOrWhiteSpace(Behavior) ? null : Behavior
    };
}

public sealed class EditorGenerator
{
    public string Id { get; set; } = "main_dungeon";
    public string Algorithm { get; set; } = "rooms_corridors";
    public int Width { get; set; } = 80;
    public int Height { get; set; } = 22;
    public int? Seed { get; set; }
    public Dictionary<string, object> Parameters { get; set; } = [];

    public static EditorGenerator FromEngine(GeneratorDefinition generator) => new()
    {
        Id = generator.Id,
        Algorithm = generator.Algorithm,
        Width = generator.Width,
        Height = generator.Height,
        Seed = generator.Seed,
        Parameters = generator.Parameters is not null
            ? new Dictionary<string, object>(generator.Parameters)
            : []
    };

    public GeneratorDefinition ToEngine() => new()
    {
        Id = Id,
        Algorithm = Algorithm,
        Width = Width,
        Height = Height,
        Seed = Seed,
        Parameters = Parameters.Count > 0 ? new Dictionary<string, object>(Parameters) : null
    };
}
