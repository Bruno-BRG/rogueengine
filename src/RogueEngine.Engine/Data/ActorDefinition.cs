namespace RogueEngine.Engine.Data;

public sealed class ActorDefinition
{
    public string Id { get; init; } = string.Empty;
    public char Glyph { get; init; }
    public ColorData Color { get; init; } = new();
    public int MaxHp { get; init; } = 1;
    public bool IsPlayer { get; init; }
    public bool BlocksMovement { get; init; }
    public bool HasChaseAI { get; init; }
    public string? Behavior { get; init; }
}
