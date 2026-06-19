namespace RogueEngine.Engine.Data;

public sealed class SaveData
{
    public int Seed { get; init; }
    public EntitySnapshot[] Entities { get; init; } = [];
}

public sealed class EntitySnapshot
{
    public string ActorId { get; init; } = string.Empty;
    public int X { get; init; }
    public int Y { get; init; }
    public int CurrentHp { get; init; }
}
