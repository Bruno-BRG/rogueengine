using RogueEngine.Engine.Core;
using RogueEngine.Engine.ProcGen;

namespace RogueEngine.Toolkit.ProcGen;

public sealed class RoomsCorridorsGenerator : IMapGenerator
{
    private const int MaxRoomAttempts = 120;
    private const int DefaultMinRoomSize = 4;
    private const int DefaultMaxRoomSize = 10;

    public string Id => "rooms_corridors";

    public DungeonGenerationResult Generate(GeneratorContext context, Random random)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(random);

        var map = new TileMap(context.Width, context.Height);
        MapCarver.FillWithWalls(map);

        var minRoom = GeneratorParameters.GetInt(context.Parameters, "minRoomSize", DefaultMinRoomSize);
        var maxRoom = GeneratorParameters.GetInt(context.Parameters, "maxRoomSize", DefaultMaxRoomSize);
        var rooms = GenerateRooms(context.Width, context.Height, random, minRoom, maxRoom).ToList();

        if (rooms.Count == 0)
        {
            var fallbackRoom = new Room(1, 1, context.Width - 2, context.Height - 2);
            MapCarver.CarveRoom(map, fallbackRoom);
            return new DungeonGenerationResult(map, [fallbackRoom]);
        }

        for (var i = 0; i < rooms.Count; i++)
        {
            MapCarver.CarveRoom(map, rooms[i]);
            if (i > 0)
            {
                MapCarver.CarveCorridor(map, rooms[i - 1].Center, rooms[i].Center);
            }
        }

        return new DungeonGenerationResult(map, rooms);
    }

    private static IEnumerable<Room> GenerateRooms(
        int width,
        int height,
        Random random,
        int minRoomSize,
        int maxRoomSize)
    {
        var rooms = new List<Room>();
        var maxSize = Math.Max(minRoomSize, maxRoomSize);

        for (var attempt = 0; attempt < MaxRoomAttempts; attempt++)
        {
            var roomWidth = random.Next(minRoomSize, maxSize + 1);
            var roomHeight = random.Next(minRoomSize, maxSize + 1);
            if (width <= roomWidth + 2 || height <= roomHeight + 2)
            {
                continue;
            }

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
}
