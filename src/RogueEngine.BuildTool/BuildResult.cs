namespace RogueEngine.BuildTool;

public sealed class BuildResult
{
    public bool Success { get; init; }
    public string OutputDirectory { get; init; } = string.Empty;
    public string? ZipPath { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = [];
}
