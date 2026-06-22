using RogueEngine.Engine.Data;

namespace RogueEngine.Engine.Tests;

public class SceneEntityRoundTripTests
{
    [Fact]
    public void SaveAndLoad_PreservesEntitiesSeedAndPlayerSpawn()
    {
        var directory = Path.Combine(Path.GetTempPath(), "rogueengine-scene-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var scenePath = Path.Combine(directory, "main.scene.json");

        try
        {
            var scene = new SceneDefinition
            {
                Id = "main",
                Name = "Test",
                Seed = 42,
                PlayerSpawnX = 3,
                PlayerSpawnY = 4,
                Entities =
                [
                    new SceneEntityPlacement { ActorId = "goblin", X = 10, Y = 11 }
                ]
            };

            SceneLoader.Save(scene, scenePath);
            var loaded = SceneLoader.Load(scenePath);

            Assert.Equal(42, loaded.Seed);
            Assert.Equal(3, loaded.PlayerSpawnX);
            Assert.Equal(4, loaded.PlayerSpawnY);
            Assert.Single(loaded.Entities);
            Assert.Equal("goblin", loaded.Entities[0].ActorId);
            Assert.Equal(10, loaded.Entities[0].X);
            Assert.Equal(11, loaded.Entities[0].Y);
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }
}
