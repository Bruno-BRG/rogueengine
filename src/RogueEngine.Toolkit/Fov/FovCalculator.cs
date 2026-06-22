using RogueEngine.Engine.Core;

namespace RogueEngine.Toolkit.Fov;

public static class FovCalculator
{
  public static bool[,] Compute(TileMap map, Position origin, int radius)
  {
    var visible = new bool[map.Width, map.Height];
    if (!map.IsInBounds(origin))
    {
      return visible;
    }

    var radiusSquared = radius * radius;
    for (var x = origin.X - radius; x <= origin.X + radius; x++)
    {
      for (var y = origin.Y - radius; y <= origin.Y + radius; y++)
      {
        if (!map.IsInBounds(x, y))
        {
          continue;
        }

        var dx = x - origin.X;
        var dy = y - origin.Y;
        if (dx * dx + dy * dy > radiusSquared)
        {
          continue;
        }

        if (HasLineOfSight(map, origin, new Position(x, y)))
        {
          visible[x, y] = true;
        }
      }
    }

    return visible;
  }

  private static bool HasLineOfSight(TileMap map, Position from, Position to)
  {
    var x0 = from.X;
    var y0 = from.Y;
    var x1 = to.X;
    var y1 = to.Y;
    var dx = Math.Abs(x1 - x0);
    var dy = Math.Abs(y1 - y0);
    var sx = x0 < x1 ? 1 : -1;
    var sy = y0 < y1 ? 1 : -1;
    var err = dx - dy;

    while (true)
    {
      if (!map.IsInBounds(x0, y0))
      {
        return false;
      }

      if (x0 == x1 && y0 == y1)
      {
        return true;
      }

      if (!(x0 == from.X && y0 == from.Y) && !map.GetTile(x0, y0).IsTransparent)
      {
        return false;
      }

      var e2 = err * 2;
      if (e2 > -dy)
      {
        err -= dy;
        x0 += sx;
      }

      if (e2 < dx)
      {
        err += dx;
        y0 += sy;
      }
    }
  }
}
