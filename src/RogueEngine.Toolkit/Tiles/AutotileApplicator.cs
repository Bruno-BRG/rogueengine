using RogueEngine.Engine.Core;
using RogueEngine.Engine.Data;

namespace RogueEngine.Toolkit.Tiles;

public static class AutotileApplicator
{
    public static void Apply(TileMap map, TileSetDefinition tileSet)
    {
        ArgumentNullException.ThrowIfNull(map);
        ArgumentNullException.ThrowIfNull(tileSet);

        bool IsWall(Tile tile) => !tile.IsWalkable;

        for (var x = 0; x < map.Width; x++)
        {
            for (var y = 0; y < map.Height; y++)
            {
                var tile = map.GetTile(x, y);
                if (tile.IsWalkable)
                {
                    continue;
                }

                var mask = TileBitmask.ComputeMask(map, x, y, IsWall);
                var glyph = ResolveGlyph(tileSet, mask);
                map.SetTile(x, y, new Tile(tile.IsWalkable, tile.IsTransparent, glyph));
            }
        }
    }

    public static char ResolveGlyph(TileSetDefinition tileSet, int mask)
    {
        var key = mask.ToString();
        if (tileSet.Tiles.TryGetValue(key, out var entry))
        {
            return entry.Glyph;
        }

        return tileSet.Tiles.TryGetValue("0", out var fallback) ? fallback.Glyph : '#';
    }
}
