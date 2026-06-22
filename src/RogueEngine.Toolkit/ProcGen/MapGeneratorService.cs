using RogueEngine.Engine.Core;
using RogueEngine.Engine.Data;
using RogueEngine.Engine.ProcGen;

namespace RogueEngine.Toolkit.ProcGen;

public static class MapGeneratorService
{
    public static DungeonGenerationResult Generate(
        int width,
        int height,
        Random random,
        string algorithm = "rooms_corridors",
        IReadOnlyDictionary<string, object>? parameters = null) =>
        GeneratorRegistry.Generate(algorithm, width, height, random, parameters);

    public static DungeonGenerationResult Generate(GeneratorDefinition definition, Random random) =>
        GeneratorRegistry.Generate(definition, random);

    public static TileMap GenerateMapOnly(GeneratorDefinition definition, Random random) =>
        Generate(definition, random).Map;
}
