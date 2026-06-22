using RogueEngine.Engine.ProcGen;

namespace RogueEngine.Toolkit.ProcGen;

public sealed class HybridCaveRoomsGenerator : IMapGenerator
{
    private readonly CellularCavesGenerator _caves = new();
    private readonly RoomsCorridorsGenerator _rooms = new();

    public string Id => "hybrid_cave_rooms";

    public DungeonGenerationResult Generate(GeneratorContext context, Random random)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(random);

        var caveResult = _caves.Generate(context, random);
        var roomContext = new GeneratorContext
        {
            Width = context.Width,
            Height = context.Height,
            Parameters = context.Parameters
        };
        var roomResult = _rooms.Generate(roomContext, random);

        foreach (var room in roomResult.Rooms.Take(3))
        {
            MapCarver.CarveRoom(caveResult.Map, room);
        }

        var mergedRooms = caveResult.Rooms.Concat(roomResult.Rooms.Take(3)).ToList();
        return new DungeonGenerationResult(caveResult.Map, mergedRooms);
    }
}
