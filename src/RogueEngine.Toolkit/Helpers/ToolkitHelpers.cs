using RogueEngine.Engine.Core;
using RogueEngine.Toolkit.ProcGen;

namespace RogueEngine.Toolkit.Helpers;

public static class MapQueries
{
    public static bool IsWalkable(TileMap map, Position position) =>
        map.IsInBounds(position) && map.IsWalkable(position);

    public static IEnumerable<Position> GetWalkableNeighbors(TileMap map, Position position)
    {
        foreach (var neighbor in GetNeighbors(position))
        {
            if (IsWalkable(map, neighbor))
            {
                yield return neighbor;
            }
        }
    }

    private static IEnumerable<Position> GetNeighbors(Position position)
    {
        yield return new Position(position.X + 1, position.Y);
        yield return new Position(position.X - 1, position.Y);
        yield return new Position(position.X, position.Y + 1);
        yield return new Position(position.X, position.Y - 1);
    }
}

public static class RandomPlacement
{
    public static Position? TryFindWalkable(TileMap map, Random random, int maxAttempts = 200)
    {
        for (var attempt = 0; attempt < maxAttempts; attempt++)
        {
            var x = random.Next(1, Math.Max(2, map.Width - 1));
            var y = random.Next(1, Math.Max(2, map.Height - 1));
            var position = new Position(x, y);
            if (map.IsWalkable(position))
            {
                return position;
            }
        }

        return null;
    }
}

public static class SeedHelper
{
    public static int FromString(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return value.GetHashCode(StringComparison.Ordinal);
    }
}
