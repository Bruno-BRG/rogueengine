namespace RogueEngine.Engine.Data;

public sealed class SceneItemPlacement
{
    public string ItemId { get; init; } = string.Empty;
    public int X { get; init; }
    public int Y { get; init; }
    public int Count { get; init; } = 1;
}
