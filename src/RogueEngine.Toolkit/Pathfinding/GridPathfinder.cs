using RogueEngine.Engine.Core;
using RogueEngine.Engine.Navigation;

namespace RogueEngine.Toolkit.Pathfinding;

public sealed class GridPathfinder : IGridNavigator
{
    public bool TryGetNextStep(TileMap map, Position from, Position to, out Position next)
    {
        next = from;
        if (from == to)
        {
            return false;
        }

        var path = FindPath(map, from, to);
        if (path is null || path.Count < 2)
        {
            return false;
        }

        next = path[1];
        return true;
    }

    public IReadOnlyList<Position>? FindPath(TileMap map, Position start, Position goal)
    {
        if (!map.IsWalkable(start) || !map.IsWalkable(goal))
        {
            return null;
        }

        var open = new PriorityQueue<Position, int>();
        var cameFrom = new Dictionary<Position, Position>();
        var gScore = new Dictionary<Position, int> { [start] = 0 };
        open.Enqueue(start, Heuristic(start, goal));

        while (open.Count > 0)
        {
            var current = open.Dequeue();
            if (current == goal)
            {
                return ReconstructPath(cameFrom, current);
            }

            foreach (var neighbor in GetNeighbors(current))
            {
                if (!map.IsInBounds(neighbor) || !map.IsWalkable(neighbor))
                {
                    continue;
                }

                var tentative = gScore[current] + 1;
                if (gScore.TryGetValue(neighbor, out var existing) && tentative >= existing)
                {
                    continue;
                }

                cameFrom[neighbor] = current;
                gScore[neighbor] = tentative;
                open.Enqueue(neighbor, tentative + Heuristic(neighbor, goal));
            }
        }

        return null;
    }

    private static IEnumerable<Position> GetNeighbors(Position position)
    {
        yield return new Position(position.X + 1, position.Y);
        yield return new Position(position.X - 1, position.Y);
        yield return new Position(position.X, position.Y + 1);
        yield return new Position(position.X, position.Y - 1);
    }

    private static int Heuristic(Position a, Position b) =>
        Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y);

    private static List<Position> ReconstructPath(Dictionary<Position, Position> cameFrom, Position current)
    {
        var path = new List<Position> { current };
        while (cameFrom.TryGetValue(current, out var previous))
        {
            current = previous;
            path.Add(current);
        }

        path.Reverse();
        return path;
    }
}
