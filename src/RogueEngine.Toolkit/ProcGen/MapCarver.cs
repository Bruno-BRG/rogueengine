using RogueEngine.Engine.Core;
using RogueEngine.Engine.ProcGen;

namespace RogueEngine.Toolkit.ProcGen;

internal static class MapCarver
{
    public static void FillWithWalls(TileMap map)
    {
        for (var x = 0; x < map.Width; x++)
        {
            for (var y = 0; y < map.Height; y++)
            {
                map.SetTile(x, y, Tile.Wall);
            }
        }
    }

    public static void CarveRoom(TileMap map, Room room)
    {
        for (var x = room.X; x < room.X + room.Width; x++)
        {
            for (var y = room.Y; y < room.Y + room.Height; y++)
            {
                if (map.IsInBounds(x, y))
                {
                    map.SetTile(x, y, Tile.Floor);
                }
            }
        }
    }

    public static void CarveCorridor(TileMap map, Position start, Position end)
    {
        var current = start;

        while (current.X != end.X)
        {
            map.SetTile(current, Tile.Floor);
            current = new Position(current.X + Math.Sign(end.X - current.X), current.Y);
        }

        while (current.Y != end.Y)
        {
            map.SetTile(current, Tile.Floor);
            current = new Position(current.X, current.Y + Math.Sign(end.Y - current.Y));
        }

        map.SetTile(end, Tile.Floor);
    }

    public static int CountWalkableTiles(TileMap map)
    {
        var count = 0;
        for (var x = 0; x < map.Width; x++)
        {
            for (var y = 0; y < map.Height; y++)
            {
                if (map.GetTile(x, y).IsWalkable)
                {
                    count++;
                }
            }
        }

        return count;
    }

    public static Position FindWalkableTile(TileMap map, Random random)
    {
        for (var attempt = 0; attempt < 500; attempt++)
        {
            var x = random.Next(1, Math.Max(2, map.Width - 1));
            var y = random.Next(1, Math.Max(2, map.Height - 1));
            if (map.IsWalkable(x, y))
            {
                return new Position(x, y);
            }
        }

        return new Position(map.Width / 2, map.Height / 2);
    }
}
