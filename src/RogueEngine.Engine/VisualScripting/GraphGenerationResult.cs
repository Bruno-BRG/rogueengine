namespace RogueEngine.Engine.VisualScripting;

public sealed class GraphGenerationResult
{
    public string Source { get; init; } = string.Empty;
    public IReadOnlyList<string> Errors { get; init; } = [];

    public bool Success => Errors.Count == 0 && !string.IsNullOrWhiteSpace(Source);
}
