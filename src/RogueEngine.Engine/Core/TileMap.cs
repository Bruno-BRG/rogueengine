namespace RogueEngine.Engine.Core;

public sealed class TileMap
{
    private readonly Tile[,] _tiles;

    public int Width { get; }
    public int Height { get; }

    public TileMap(int width, int height)
    {
        if (width <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width));
        }

        if (height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(height));
        }

        Width = width;
        Height = height;
        _tiles = new Tile[width, height];
    }

    public bool IsInBounds(int x, int y) =>
        x >= 0 && x < Width && y >= 0 && y < Height;

    public bool IsInBounds(Position position) =>
        IsInBounds(position.X, position.Y);

    public Tile GetTile(int x, int y)
    {
        if (!IsInBounds(x, y))
        {
            throw new ArgumentOutOfRangeException($"Position ({x}, {y}) is outside the map bounds.");
        }

        return _tiles[x, y];
    }

    public Tile GetTile(Position position) => GetTile(position.X, position.Y);

    public void SetTile(int x, int y, Tile tile)
    {
        if (!IsInBounds(x, y))
        {
            throw new ArgumentOutOfRangeException($"Position ({x}, {y}) is outside the map bounds.");
        }

        _tiles[x, y] = tile;
    }

    public void SetTile(Position position, Tile tile) =>
        SetTile(position.X, position.Y, tile);

    public bool IsWalkable(int x, int y) =>
        IsInBounds(x, y) && GetTile(x, y).IsWalkable;

    public bool IsWalkable(Position position) =>
        IsWalkable(position.X, position.Y);
}
