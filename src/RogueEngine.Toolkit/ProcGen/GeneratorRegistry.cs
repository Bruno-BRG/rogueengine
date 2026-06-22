using RogueEngine.Engine.Data;
using RogueEngine.Engine.ProcGen;

namespace RogueEngine.Toolkit.ProcGen;

public static class GeneratorRegistry
{
    private static readonly Dictionary<string, IMapGenerator> Generators =
        new(StringComparer.OrdinalIgnoreCase);

    static GeneratorRegistry()
    {
        Register(new RoomsCorridorsGenerator());
        Register(new CellularCavesGenerator());
        Register(new DrunkardWalkGenerator());
        Register(new BspDungeonGenerator());
        Register(new HybridCaveRoomsGenerator());
    }

    public static void Register(IMapGenerator generator)
    {
        ArgumentNullException.ThrowIfNull(generator);
        Generators[generator.Id] = generator;
    }

    public static IMapGenerator Get(string algorithmId)
    {
        if (!Generators.TryGetValue(algorithmId, out var generator))
        {
            throw new NotSupportedException($"Map generator algorithm '{algorithmId}' is not registered.");
        }

        return generator;
    }

    public static bool TryGet(string algorithmId, out IMapGenerator? generator) =>
        Generators.TryGetValue(algorithmId, out generator);

    public static IReadOnlyList<string> List() => Generators.Keys.OrderBy(id => id, StringComparer.OrdinalIgnoreCase).ToList();

    public static DungeonGenerationResult Generate(
        string algorithmId,
        int width,
        int height,
        Random random,
        IReadOnlyDictionary<string, object>? parameters = null)
    {
        var generator = Get(algorithmId);
        var context = new GeneratorContext
        {
            Width = width,
            Height = height,
            Parameters = parameters ?? new Dictionary<string, object>()
        };
        return generator.Generate(context, random);
    }

    public static DungeonGenerationResult Generate(GeneratorDefinition definition, Random random)
    {
        ArgumentNullException.ThrowIfNull(definition);
        return Generate(
            definition.Algorithm,
            definition.Width,
            definition.Height,
            random,
            definition.Parameters);
    }
}
