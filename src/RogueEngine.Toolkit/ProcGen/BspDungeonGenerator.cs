using RogueEngine.Engine.Core;
using RogueEngine.Engine.ProcGen;

namespace RogueEngine.Toolkit.ProcGen;

public sealed class BspDungeonGenerator : IMapGenerator
{
    public string Id => "bsp_dungeon";

    public DungeonGenerationResult Generate(GeneratorContext context, Random random)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(random);

        var minRoomSize = GeneratorParameters.GetInt(context.Parameters, "minRoomSize", 4);
        var maxRoomSize = GeneratorParameters.GetInt(context.Parameters, "maxRoomSize", 10);
        var splitDepth = GeneratorParameters.GetInt(context.Parameters, "splitDepth", 4);

        var map = new TileMap(context.Width, context.Height);
        MapCarver.FillWithWalls(map);

        var root = new BspNode(1, 1, context.Width - 2, context.Height - 2);
        Split(root, random, splitDepth, minRoomSize);
        var rooms = new List<Room>();
        CreateRooms(root, random, minRoomSize, maxRoomSize, rooms);

        if (rooms.Count == 0)
        {
            var fallback = new Room(2, 2, context.Width - 4, context.Height - 4);
            MapCarver.CarveRoom(map, fallback);
            return new DungeonGenerationResult(map, [fallback]);
        }

        foreach (var room in rooms)
        {
            MapCarver.CarveRoom(map, room);
        }

        ConnectChildren(map, root);

        return new DungeonGenerationResult(map, rooms);
    }

    private static void Split(BspNode node, Random random, int depth, int minSize)
    {
        if (depth <= 0 || node.Width < minSize * 2 + 2 || node.Height < minSize * 2 + 2)
        {
            return;
        }

        var splitHorizontal = node.Width < node.Height
            ? true
            : node.Height < node.Width
                ? false
                : random.Next(2) == 0;

        if (splitHorizontal)
        {
            var splitAt = random.Next(minSize, node.Height - minSize);
            node.Left = new BspNode(node.X, node.Y, node.Width, splitAt);
            node.Right = new BspNode(node.X, node.Y + splitAt, node.Width, node.Height - splitAt);
        }
        else
        {
            var splitAt = random.Next(minSize, node.Width - minSize);
            node.Left = new BspNode(node.X, node.Y, splitAt, node.Height);
            node.Right = new BspNode(node.X + splitAt, node.Y, node.Width - splitAt, node.Height);
        }

        Split(node.Left, random, depth - 1, minSize);
        Split(node.Right, random, depth - 1, minSize);
    }

    private static void CreateRooms(
        BspNode node,
        Random random,
        int minRoomSize,
        int maxRoomSize,
        List<Room> rooms)
    {
        if (node.Left is null || node.Right is null)
        {
            var roomWidth = random.Next(minRoomSize, Math.Min(maxRoomSize, node.Width - 1) + 1);
            var roomHeight = random.Next(minRoomSize, Math.Min(maxRoomSize, node.Height - 1) + 1);
            var x = node.X + random.Next(0, Math.Max(1, node.Width - roomWidth));
            var y = node.Y + random.Next(0, Math.Max(1, node.Height - roomHeight));
            var room = new Room(x, y, roomWidth, roomHeight);
            node.Room = room;
            rooms.Add(room);
            return;
        }

        CreateRooms(node.Left, random, minRoomSize, maxRoomSize, rooms);
        CreateRooms(node.Right, random, minRoomSize, maxRoomSize, rooms);
    }

    private static void ConnectChildren(TileMap map, BspNode node)
    {
        if (node.Left is null || node.Right is null)
        {
            return;
        }

        ConnectChildren(map, node.Left);
        ConnectChildren(map, node.Right);

        var leftCenter = GetRoomCenter(node.Left);
        var rightCenter = GetRoomCenter(node.Right);
        MapCarver.CarveCorridor(map, leftCenter, rightCenter);
    }

    private static Position GetRoomCenter(BspNode node)
    {
        if (node.Room is Room room)
        {
            return room.Center;
        }

        if (node.Left is not null)
        {
            return GetRoomCenter(node.Left);
        }

        return new Position(node.X + node.Width / 2, node.Y + node.Height / 2);
    }

    private sealed class BspNode(int x, int y, int width, int height)
    {
        public int X { get; } = x;
        public int Y { get; } = y;
        public int Width { get; } = width;
        public int Height { get; } = height;
        public BspNode? Left { get; set; }
        public BspNode? Right { get; set; }
        public Room? Room { get; set; }
    }
}
