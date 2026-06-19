using RogueEngine.Engine.Components;
using RogueEngine.Engine.Core;
using RogueEngine.Engine.Data;

namespace RogueEngine.Engine.Tests;

public class GameSaveLoadTests
{
    [Fact]
    public void SaveAndLoad_PreservesSeedAndEntityState()
    {
        var world = CreateWorldWithEntities();
        var savePath = Path.Combine(Path.GetTempPath(), $"rogueengine-save-{Guid.NewGuid():N}.json");

        try
        {
            GameSaveLoad.Save(world, 1234, savePath);
            var saveData = GameSaveLoad.LoadSaveData(savePath);

            Assert.Equal(1234, saveData.Seed);
            Assert.Equal(2, saveData.Entities.Length);

            var playerSnapshot = saveData.Entities.Single(entity => entity.ActorId == "player");
            Assert.Equal(10, playerSnapshot.X);
            Assert.Equal(11, playerSnapshot.Y);
            Assert.Equal(18, playerSnapshot.CurrentHp);

            var goblinSnapshot = saveData.Entities.Single(entity => entity.ActorId == "goblin");
            Assert.Equal(12, goblinSnapshot.X);
            Assert.Equal(11, goblinSnapshot.Y);
            Assert.Equal(4, goblinSnapshot.CurrentHp);
        }
        finally
        {
            if (File.Exists(savePath))
            {
                File.Delete(savePath);
            }
        }
    }

    [Fact]
    public void TryLoadSaveData_ReturnsFalse_WhenFileMissing()
    {
        var found = GameSaveLoad.TryLoadSaveData("missing-save.json", out var saveData);

        Assert.False(found);
        Assert.Null(saveData);
    }

    private static World CreateWorldWithEntities()
    {
        var map = new TileMap(20, 20);
        for (var x = 0; x < map.Width; x++)
        {
            for (var y = 0; y < map.Height; y++)
            {
                map.SetTile(x, y, Tile.Floor);
            }
        }

        var world = new World(map);

        var player = new Entity();
        player.AddComponent(new ActorIdComponent("player"));
        player.AddComponent(new PositionComponent(new Position(10, 11)));
        player.AddComponent(new RenderableComponent('@', RgbColor.Yellow));
        player.AddComponent(new HealthComponent(20, 18));
        player.AddComponent(new IsPlayerComponent());
        world.AddEntity(player);

        var goblin = new Entity();
        goblin.AddComponent(new ActorIdComponent("goblin"));
        goblin.AddComponent(new PositionComponent(new Position(12, 11)));
        goblin.AddComponent(new RenderableComponent('o', RgbColor.Gray));
        goblin.AddComponent(new HealthComponent(6, 4));
        goblin.AddComponent(new BlocksMovementComponent());
        world.AddEntity(goblin);

        return world;
    }
}
