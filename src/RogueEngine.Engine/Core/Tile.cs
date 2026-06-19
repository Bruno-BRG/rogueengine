namespace RogueEngine.Engine.Core;

public readonly record struct Tile(bool IsWalkable, bool IsTransparent, char Glyph)
{
    public static Tile Floor => new(true, true, '.');
    public static Tile Wall => new(false, false, '#');
}
