using RogueEngine.Editor.Models;
using RogueEngine.Engine.Core;
using RogueEngine.Engine.Data;
using RogueEngine.Toolkit.ProcGen;

namespace RogueEngine.Editor.Services;

public sealed class MapPreviewResult
{
    public required TileMap Map { get; init; }
    public int Seed { get; init; }
}

public sealed class MapPreviewService
{
    public MapPreviewResult Generate(EditorScene scene, EditorGenerator generator, int? seedOverride = null)
    {
        ArgumentNullException.ThrowIfNull(scene);
        ArgumentNullException.ThrowIfNull(generator);

        var width = scene.Width ?? generator.Width;
        var height = scene.Height ?? generator.Height;
        var seed = seedOverride ?? scene.Seed ?? generator.Seed ?? Random.Shared.Next();
        var random = new Random(seed);
        var definition = new GeneratorDefinition
        {
            Id = generator.Id,
            Algorithm = generator.Algorithm,
            Width = width,
            Height = height,
            Seed = seed,
            Parameters = generator.Parameters.Count > 0
                ? new Dictionary<string, object>(generator.Parameters)
                : null
        };
        var map = MapGeneratorService.GenerateMapOnly(definition, random);
        return new MapPreviewResult { Map = map, Seed = seed };
    }
}
