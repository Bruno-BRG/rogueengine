namespace RogueEngine.Engine.Data;

public sealed class GameSettings
{
    public int MapWidth { get; init; } = 80;
    public int MapHeight { get; init; } = 22;
    public int MessagePanelHeight { get; init; } = 3;
    public int MinEnemies { get; init; } = 3;
    public int MaxEnemies { get; init; } = 5;

    public int WindowWidth => MapWidth;
    public int WindowHeight => MapHeight + MessagePanelHeight;
}
