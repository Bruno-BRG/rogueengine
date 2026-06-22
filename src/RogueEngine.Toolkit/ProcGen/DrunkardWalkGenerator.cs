using RogueEngine.Engine.Core;
using RogueEngine.Engine.ProcGen;

namespace RogueEngine.Toolkit.ProcGen;

public sealed class DrunkardWalkGenerator : IMapGenerator
{
    public string Id => "drunkard_walk";

    public DungeonGenerationResult Generate(GeneratorContext context, Random random)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(random);

        var targetFloorCount = GeneratorParameters.GetInt(
            context.Parameters,
            "targetFloorCount",
            Math.Max(80, context.Width * context.Height / 8));
        var walkerCount = GeneratorParameters.GetInt(context.Parameters, "walkerCount", 3);

        var map = new TileMap(context.Width, context.Height);
        MapCarver.FillWithWalls(map);

        var carved = 0;
        for (var w = 0; w < walkerCount && carved < targetFloorCount; w++)
        {
            var position = new Position(random.Next(1, map.Width - 1), random.Next(1, map.Height - 1));
            var steps = targetFloorCount / walkerCount;
            for (var step = 0; step < steps && carved < targetFloorCount; step++)
            {
                if (map.IsInBounds(position) && !map.IsWalkable(position))
                {
                    map.SetTile(position, Tile.Floor);
                    carved++;
                }

                position = position + RandomStep(random);
            }
        }

        if (carved == 0)
        {
            var fallback = new Room(2, 2, context.Width - 4, context.Height - 4);
            MapCarver.CarveRoom(map, fallback);
            return new DungeonGenerationResult(map, [fallback]);
        }

        var room = new Room(2, 2, Math.Max(4, context.Width / 4), Math.Max(4, context.Height / 4));
        return new DungeonGenerationResult(map, [room]);
    }

    private static Position RandomStep(Random random)
    {
        return random.Next(4) switch
        {
            0 => new Position(1, 0),
            1 => new Position(-1, 0),
            2 => new Position(0, 1),
            _ => new Position(0, -1)
        };
    }
}
