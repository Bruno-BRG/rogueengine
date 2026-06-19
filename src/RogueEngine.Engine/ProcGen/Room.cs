using RogueEngine.Engine.Core;

namespace RogueEngine.Engine.ProcGen;

public readonly record struct Room(int X, int Y, int Width, int Height)
{
    public Position Center => new(X + Width / 2, Y + Height / 2);

    public bool Intersects(Room other, int padding = 1) =>
        X - padding <= other.X + other.Width &&
        X + Width + padding >= other.X &&
        Y - padding <= other.Y + other.Height &&
        Y + Height + padding >= other.Y;
}
