namespace RogueEngine.Engine.Data;

public sealed class GeneratorDefinition
{
    public string Id { get; init; } = string.Empty;
    public string Algorithm { get; init; } = "rooms_corridors";
    public int Width { get; init; } = 80;
    public int Height { get; init; } = 22;
    public int? Seed { get; init; }
    public Dictionary<string, object>? Parameters { get; init; }
}
