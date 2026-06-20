namespace RogueEngine.Engine.VisualScripting;

public static class NodeType
{
    public const string OnTurn = "OnTurn";
    public const string IsPlayerAdjacent = "IsPlayerAdjacent";
    public const string HasHpBelow = "HasHpBelow";
    public const string MoveTowardPlayer = "MoveTowardPlayer";
    public const string AttackAtPlayer = "AttackAtPlayer";
    public const string Log = "Log";

    public static IReadOnlySet<string> All { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        OnTurn,
        IsPlayerAdjacent,
        HasHpBelow,
        MoveTowardPlayer,
        AttackAtPlayer,
        Log
    };

    public static bool IsCondition(string type) =>
        type.Equals(IsPlayerAdjacent, StringComparison.OrdinalIgnoreCase) ||
        type.Equals(HasHpBelow, StringComparison.OrdinalIgnoreCase);

    public static bool IsEvent(string type) =>
        type.Equals(OnTurn, StringComparison.OrdinalIgnoreCase);
}
