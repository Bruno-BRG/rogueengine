namespace RogueEngine.Engine.Data;

public sealed class ClassStatBonuses
{
    public int Attack { get; init; }
    public int Defense { get; init; }
    public int MaxHp { get; init; }
}

public sealed class ClassStartItem
{
    public string ItemId { get; init; } = string.Empty;
    public int Count { get; init; } = 1;
}

public sealed class ClassDefinition
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public int BaseMaxHp { get; init; }
    public string? PlayerActorId { get; init; }
    public ClassStatBonuses StatBonuses { get; init; } = new();
    public IReadOnlyList<ClassStartItem> StartItems { get; init; } = [];
}
