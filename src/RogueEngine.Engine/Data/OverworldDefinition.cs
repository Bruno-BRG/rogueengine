namespace RogueEngine.Engine.Data;

public sealed class OverworldDefinition
{
    public string Id { get; init; } = string.Empty;
    public IReadOnlyList<OverworldCellDefinition> Cells { get; init; } = [];
    public IReadOnlyList<OverworldConnectionDefinition> Connections { get; init; } = [];
}

public sealed class OverworldCellDefinition
{
    public string Id { get; init; } = string.Empty;
    public int X { get; init; }
    public int Y { get; init; }
    public string Biome { get; init; } = string.Empty;
    public string LocalGenerator { get; init; } = string.Empty;
}

public sealed class OverworldConnectionDefinition
{
    public string From { get; init; } = string.Empty;
    public string To { get; init; } = string.Empty;
    public string Type { get; init; } = "road";
}
