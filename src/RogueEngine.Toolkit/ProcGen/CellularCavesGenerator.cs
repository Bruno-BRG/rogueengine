using RogueEngine.Engine.Core;
using RogueEngine.Engine.ProcGen;

namespace RogueEngine.Toolkit.ProcGen;

public sealed class CellularCavesGenerator : IMapGenerator
{
    public string Id => "cellular_caves";

    public DungeonGenerationResult Generate(GeneratorContext context, Random random)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(random);

        var fillPercent = GeneratorParameters.GetDouble(context.Parameters, "fillPercent", 0.45);
        var smoothPasses = GeneratorParameters.GetInt(context.Parameters, "smoothPasses", 5);
        var wallThreshold = GeneratorParameters.GetInt(context.Parameters, "wallThreshold", 4);

        var map = new TileMap(context.Width, context.Height);
        var solid = new bool[context.Width, context.Height];

        for (var x = 0; x < context.Width; x++)
        {
            for (var y = 0; y < context.Height; y++)
            {
                var edge = x == 0 || y == 0 || x == context.Width - 1 || y == context.Height - 1;
                solid[x, y] = edge || random.NextDouble() < fillPercent;
            }
        }

        for (var pass = 0; pass < smoothPasses; pass++)
        {
            solid = Smooth(solid, context.Width, context.Height, wallThreshold);
        }

        ApplySolidToMap(map, solid);

        if (MapCarver.CountWalkableTiles(map) < 10)
        {
            var center = new Room(2, 2, context.Width - 4, context.Height - 4);
            MapCarver.CarveRoom(map, center);
            return new DungeonGenerationResult(map, [center]);
        }

        var room = new Room(2, 2, Math.Max(4, context.Width / 3), Math.Max(4, context.Height / 3));
        return new DungeonGenerationResult(map, [room]);
    }

    private static bool[,] Smooth(bool[,] solid, int width, int height, int wallThreshold)
    {
        var next = new bool[width, height];
        for (var x = 0; x < width; x++)
        {
            for (var y = 0; y < height; y++)
            {
                var wallNeighbors = CountWallNeighbors(solid, width, height, x, y);
                next[x, y] = wallNeighbors >= wallThreshold;
            }
        }

        return next;
    }

    private static int CountWallNeighbors(bool[,] solid, int width, int height, int x, int y)
    {
        var count = 0;
        for (var dx = -1; dx <= 1; dx++)
        {
            for (var dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0)
                {
                    continue;
                }

                var nx = x + dx;
                var ny = y + dy;
                if (nx < 0 || ny < 0 || nx >= width || ny >= height || solid[nx, ny])
                {
                    count++;
                }
            }
        }

        return count;
    }

    private static void ApplySolidToMap(TileMap map, bool[,] solid)
    {
        for (var x = 0; x < map.Width; x++)
        {
            for (var y = 0; y < map.Height; y++)
            {
                map.SetTile(x, y, solid[x, y] ? Tile.Wall : Tile.Floor);
            }
        }
    }
}
