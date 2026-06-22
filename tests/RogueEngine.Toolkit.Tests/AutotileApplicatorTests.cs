using RogueEngine.Engine.Core;
using RogueEngine.Toolkit.Tiles;

namespace RogueEngine.Toolkit.Tests;

public class AutotileApplicatorTests
{
    [Fact]
    public void Apply_ReplacesWallGlyphsUsingMask()
    {
        var map = new TileMap(3, 3);
        for (var x = 0; x < 3; x++)
        {
            for (var y = 0; y < 3; y++)
            {
                map.SetTile(x, y, x == 1 && y == 1 ? Tile.Floor : Tile.Wall);
            }
        }

        var tileSet = new Engine.Data.TileSetDefinition
        {
            Id = "test",
            Tiles =
            {
                ["0"] = new Engine.Data.TileSetGlyphEntry { Glyph = '.' },
                ["15"] = new Engine.Data.TileSetGlyphEntry { Glyph = '+' }
            }
        };

        AutotileApplicator.Apply(map, tileSet);
        Assert.Equal('+', map.GetTile(0, 0).Glyph);
    }
}
