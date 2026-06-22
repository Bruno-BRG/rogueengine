namespace RogueEngine.Engine.Core;

public sealed class MapVisibility
{
    private readonly bool[,] _explored;
    private readonly bool[,] _visible;

    public int Width { get; }
    public int Height { get; }

    public MapVisibility(int width, int height)
    {
        Width = width;
        Height = height;
        _explored = new bool[width, height];
        _visible = new bool[width, height];
    }

    public bool IsExplored(int x, int y) => IsInBounds(x, y) && _explored[x, y];

    public bool IsVisible(int x, int y) => IsInBounds(x, y) && _visible[x, y];

    public void ClearVisible()
    {
        Array.Clear(_visible, 0, _visible.Length);
    }

    public void ApplyVisible(bool[,] visibleTiles)
    {
        ClearVisible();
        for (var x = 0; x < Width; x++)
        {
            for (var y = 0; y < Height; y++)
            {
                if (visibleTiles[x, y])
                {
                    _visible[x, y] = true;
                    _explored[x, y] = true;
                }
            }
        }
    }

    private bool IsInBounds(int x, int y) => x >= 0 && x < Width && y >= 0 && y < Height;
}
