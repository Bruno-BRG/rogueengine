namespace RogueEngine.Engine.Data;

public sealed class GameProject
{
    public string Name { get; init; } = string.Empty;
    public string Version { get; init; } = string.Empty;
    public string DataPath { get; init; } = "Data";
    public string? DefaultGenerator { get; init; }
}
