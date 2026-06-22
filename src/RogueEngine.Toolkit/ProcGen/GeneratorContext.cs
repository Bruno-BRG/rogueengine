namespace RogueEngine.Toolkit.ProcGen;

public sealed class GeneratorContext
{
    public required int Width { get; init; }
    public required int Height { get; init; }
    public IReadOnlyDictionary<string, object> Parameters { get; init; } =
        new Dictionary<string, object>();
}
