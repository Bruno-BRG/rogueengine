namespace RogueEngine.Engine.Data;

/// <summary>
/// A playable level / dungeon scene. Maps to <c>Data/scenes/*.scene.json</c>.
/// </summary>
public sealed class SceneDefinition
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? Generator { get; init; }
    public int? Width { get; init; }
    public int? Height { get; init; }
    public int? Seed { get; init; }
    public int? PlayerSpawnX { get; init; }
    public int? PlayerSpawnY { get; init; }
    public IReadOnlyList<SceneEntityPlacement> Entities { get; init; } = [];
    public IReadOnlyList<SceneItemPlacement> ItemPlacements { get; init; } = [];
    public IReadOnlyList<SceneInteractionPlacement> Interactions { get; init; } = [];
}
