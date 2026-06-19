namespace RogueEngine.Engine.Core;

public readonly record struct Position(int X, int Y)
{
    public static Position operator +(Position left, Position right) =>
        new(left.X + right.X, left.Y + right.Y);

    public static Position operator -(Position left, Position right) =>
        new(left.X - right.X, left.Y - right.Y);
}
