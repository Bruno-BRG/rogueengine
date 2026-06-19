using RogueEngine.Engine.Core;

namespace RogueEngine.Engine.Tests;

public class TileMapTests
{
    [Fact]
    public void IsInBounds_ReturnsTrue_ForValidCoordinates()
    {
        var map = new TileMap(10, 8);

        Assert.True(map.IsInBounds(0, 0));
        Assert.True(map.IsInBounds(9, 7));
    }

    [Fact]
    public void IsInBounds_ReturnsFalse_ForOutOfBoundsCoordinates()
    {
        var map = new TileMap(10, 8);

        Assert.False(map.IsInBounds(-1, 0));
        Assert.False(map.IsInBounds(10, 0));
        Assert.False(map.IsInBounds(0, 8));
    }

    [Fact]
    public void IsWalkable_ReturnsFalse_ForOutOfBounds()
    {
        var map = new TileMap(5, 5);

        Assert.False(map.IsWalkable(-1, 0));
        Assert.False(map.IsWalkable(5, 5));
    }

    [Fact]
    public void SetTile_AndGetTile_RoundTrip()
    {
        var map = new TileMap(5, 5);
        map.SetTile(2, 3, Tile.Wall);

        var tile = map.GetTile(2, 3);

        Assert.Equal('#', tile.Glyph);
        Assert.False(tile.IsWalkable);
    }

    [Fact]
    public void IsWalkable_ReflectsTileWalkability()
    {
        var map = new TileMap(3, 3);
        map.SetTile(1, 1, Tile.Floor);
        map.SetTile(0, 0, Tile.Wall);

        Assert.True(map.IsWalkable(1, 1));
        Assert.False(map.IsWalkable(0, 0));
    }
}
