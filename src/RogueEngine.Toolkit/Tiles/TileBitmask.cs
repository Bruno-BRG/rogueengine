using RogueEngine.Engine.Core;

namespace RogueEngine.Toolkit.Tiles;

public static class TileBitmask
{
    public static int ComputeMask(TileMap map, int x, int y, Func<Tile, bool> isSolid)
    {
        var mask = 0;
        if (IsSolid(map, x, y - 1, isSolid))
        {
            mask |= 1;
        }

        if (IsSolid(map, x + 1, y, isSolid))
        {
            mask |= 2;
        }

        if (IsSolid(map, x, y + 1, isSolid))
        {
            mask |= 4;
        }

        if (IsSolid(map, x - 1, y, isSolid))
        {
            mask |= 8;
        }

        return mask;
    }

    private static bool IsSolid(TileMap map, int x, int y, Func<Tile, bool> isSolid)
    {
        if (!map.IsInBounds(x, y))
        {
            return true;
        }

        return isSolid(map.GetTile(x, y));
    }
}
