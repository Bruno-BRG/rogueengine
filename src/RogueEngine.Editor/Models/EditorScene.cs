using RogueEngine.Engine.Data;

namespace RogueEngine.Editor.Models;

public sealed class EditorScene
{
    public string Id { get; set; } = string.Empty;
    public string SourceFileName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Generator { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
    public int? PlayerSpawnX { get; set; }
    public int? PlayerSpawnY { get; set; }

    public static EditorScene FromEngine(SceneDefinition scene, string fileName) => new()
    {
        Id = scene.Id,
        SourceFileName = fileName,
        Name = scene.Name,
        Generator = scene.Generator,
        Width = scene.Width,
        Height = scene.Height,
        PlayerSpawnX = scene.PlayerSpawnX,
        PlayerSpawnY = scene.PlayerSpawnY
    };

    public SceneDefinition ToEngine() => new()
    {
        Id = Id,
        Name = Name,
        Generator = Generator,
        Width = Width,
        Height = Height,
        PlayerSpawnX = PlayerSpawnX,
        PlayerSpawnY = PlayerSpawnY
    };
}

public sealed class EditorScriptFile
{
    public required string FileName { get; init; }
    public required string FullPath { get; init; }
    public string Content { get; set; } = string.Empty;
}
