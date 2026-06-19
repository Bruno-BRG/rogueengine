using RogueEngine.Engine.Core;

namespace RogueEngine.Engine.ProcGen;

public readonly record struct DungeonGenerationResult(TileMap Map, IReadOnlyList<Room> Rooms);

public sealed class DungeonGenerator
{
    private const int MaxRoomAttempts = 120;
    private const int MinRoomSize = 4;
    private const int MaxRoomSize = 10;

    public DungeonGenerationResult Generate(int width, int height, Random random)
    {
        ArgumentNullException.ThrowIfNull(random);

        var map = new TileMap(width, height);
        FillWithWalls(map);

        var rooms = GenerateRooms(width, height, random).ToList();
        if (rooms.Count == 0)
        {
            var fallbackRoom = new Room(1, 1, width - 2, height - 2);
            CarveRoom(map, fallbackRoom);
            return new DungeonGenerationResult(map, [fallbackRoom]);
        }

        for (var i = 0; i < rooms.Count; i++)
        {
            CarveRoom(map, rooms[i]);
            if (i > 0)
            {
                CarveCorridor(map, rooms[i - 1].Center, rooms[i].Center);
            }
        }

        return new DungeonGenerationResult(map, rooms);
    }

    public TileMap GenerateMapOnly(int width, int height, Random random) =>
        Generate(width, height, random).Map;

    public IReadOnlyList<Room> GenerateRooms(int width, int height, Random random)
    {
        var rooms = new List<Room>();

        for (var attempt = 0; attempt < MaxRoomAttempts; attempt++)
        {
            var roomWidth = random.Next(MinRoomSize, MaxRoomSize + 1);
            var roomHeight = random.Next(MinRoomSize, MaxRoomSize + 1);
            var x = random.Next(1, width - roomWidth - 1);
            var y = random.Next(1, height - roomHeight - 1);
            var candidate = new Room(x, y, roomWidth, roomHeight);

            if (rooms.Any(existing => existing.Intersects(candidate)))
            {
                continue;
            }

            rooms.Add(candidate);
        }

        return rooms;
    }

    private static void FillWithWalls(TileMap map)
    {
        for (var x = 0; x < map.Width; x++)
        {
            for (var y = 0; y < map.Height; y++)
            {
                map.SetTile(x, y, Tile.Wall);
            }
        }
    }

    private static void CarveRoom(TileMap map, Room room)
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

    private static void CarveCorridor(TileMap map, Position start, Position end)
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
}
