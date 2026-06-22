namespace RogueEngine.Engine.Data;

public sealed class InteractionDefinition
{
    public string Id { get; init; } = string.Empty;
    public string Kind { get; init; } = "use";
    public string? RequiredKeyId { get; init; }
    public string? TargetScene { get; init; }
    public string? Message { get; init; }
    public string? Script { get; init; }
}
