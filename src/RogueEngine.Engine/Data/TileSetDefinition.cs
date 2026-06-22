namespace RogueEngine.Engine.Data;

public sealed class TileSetDefinition
{
    public string Id { get; init; } = string.Empty;
    public string MatchMode { get; init; } = "cardinal4";
    public Dictionary<string, TileSetGlyphEntry> Tiles { get; init; } = [];
}

public sealed class TileSetGlyphEntry
{
    public char Glyph { get; init; } = '#';
}
