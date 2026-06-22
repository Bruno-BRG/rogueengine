using RogueEngine.Engine.ProcGen;

namespace RogueEngine.Toolkit.ProcGen;

public interface IMapGenerator
{
    string Id { get; }
    DungeonGenerationResult Generate(GeneratorContext context, Random random);
}
