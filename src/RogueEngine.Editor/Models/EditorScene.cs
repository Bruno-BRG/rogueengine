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
    public int? Seed { get; set; }
    public int? PlayerSpawnX { get; set; }
    public int? PlayerSpawnY { get; set; }
    public List<EditorSceneEntity> Entities { get; set; } = [];
    public List<EditorSceneItem> ItemPlacements { get; set; } = [];
    public List<EditorSceneInteraction> InteractionPlacements { get; set; } = [];

    public static EditorScene FromEngine(SceneDefinition scene, string fileName) => new()
    {
        Id = scene.Id,
        SourceFileName = fileName,
        Name = scene.Name,
        Generator = scene.Generator,
        Width = scene.Width,
        Height = scene.Height,
        Seed = scene.Seed,
        PlayerSpawnX = scene.PlayerSpawnX,
        PlayerSpawnY = scene.PlayerSpawnY,
        Entities = scene.Entities
            .Select(entity => new EditorSceneEntity
            {
                ActorId = entity.ActorId,
                X = entity.X,
                Y = entity.Y
            })
            .ToList(),
        ItemPlacements = scene.ItemPlacements
            .Select(item => new EditorSceneItem
            {
                ItemId = item.ItemId,
                X = item.X,
                Y = item.Y,
                Count = item.Count
            })
            .ToList(),
        InteractionPlacements = scene.Interactions
            .Select(interaction => new EditorSceneInteraction
            {
                InteractionId = interaction.InteractionId,
                X = interaction.X,
                Y = interaction.Y
            })
            .ToList()
    };

    public SceneDefinition ToEngine() => new()
    {
        Id = Id,
        Name = Name,
        Generator = Generator,
        Width = Width,
        Height = Height,
        Seed = Seed,
        PlayerSpawnX = PlayerSpawnX,
        PlayerSpawnY = PlayerSpawnY,
        Entities = Entities
            .Select(entity => new SceneEntityPlacement
            {
                ActorId = entity.ActorId,
                X = entity.X,
                Y = entity.Y
            })
            .ToList(),
        ItemPlacements = ItemPlacements
            .Select(item => new SceneItemPlacement
            {
                ItemId = item.ItemId,
                X = item.X,
                Y = item.Y,
                Count = item.Count
            })
            .ToList(),
        Interactions = InteractionPlacements
            .Select(interaction => new SceneInteractionPlacement
            {
                InteractionId = interaction.InteractionId,
                X = interaction.X,
                Y = interaction.Y
            })
            .ToList()
    };
}

public sealed class EditorScriptFile
{
    public required string FileName { get; init; }
    public required string FullPath { get; init; }
    public string Content { get; set; } = string.Empty;
}
