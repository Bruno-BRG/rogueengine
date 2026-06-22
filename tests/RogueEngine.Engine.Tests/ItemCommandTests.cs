using RogueEngine.Engine.Commands;
using RogueEngine.Engine.Components;
using RogueEngine.Engine.Core;
using RogueEngine.Engine.Data;

namespace RogueEngine.Engine.Tests;

public class ItemCommandTests
{
    [Fact]
    public void PickupCommand_AddsItemToInventory()
    {
        var world = new World(new TileMap(5, 5));
        var player = CreatePlayer(world, new Position(1, 1));
        var pickup = new Entity();
        pickup.AddComponent(new PositionComponent(new Position(2, 1)));
        pickup.AddComponent(new ItemPickupComponent { ItemId = "health_potion", Count = 2 });
        world.AddEntity(pickup);

        var command = new PickupCommand(player);
        Assert.True(command.Execute(world));

        var inventory = player.GetComponent<InventoryComponent>();
        Assert.Single(inventory.Stacks);
        Assert.Equal("health_potion", inventory.Stacks[0].ItemId);
        Assert.Equal(2, inventory.Stacks[0].Count);
    }

    [Fact]
    public void UseItemCommand_HealsPlayerAndConsumesStack()
    {
        var world = new World(new TileMap(5, 5));
        var player = CreatePlayer(world, new Position(1, 1));
        var inventory = player.GetComponent<InventoryComponent>();
        inventory.Stacks.Add(new InventoryStack { ItemId = "health_potion", Count = 1 });
        var health = player.GetComponent<HealthComponent>();
        health.TakeDamage(4);

        var items = new Dictionary<string, ItemDefinition>
        {
            ["health_potion"] = new()
            {
                Id = "health_potion",
                Name = "Health Potion",
                Kind = "consumable",
                OnUse = new ItemUseEffect { Heal = 5 }
            }
        };

        var command = new UseItemCommand(player, 1);
        Assert.True(command.Execute(world, items));
        Assert.Equal(health.MaxHp, health.CurrentHp);
        Assert.Empty(inventory.Stacks);
    }

    [Fact]
    public void GameSaveLoad_RoundTripsInventory()
    {
        var world = new World(new TileMap(5, 5));
        var player = CreatePlayer(world, new Position(1, 1));
        var inventory = player.GetComponent<InventoryComponent>();
        inventory.Stacks.Add(new InventoryStack { ItemId = "health_potion", Count = 3 });
        inventory.EquippedWeaponId = "rusty_sword";

        var savePath = Path.Combine(Path.GetTempPath(), $"rogue-save-{Guid.NewGuid():N}.json");
        try
        {
            GameSaveLoad.Save(world, 42, savePath);
            var loaded = GameSaveLoad.LoadSaveData(savePath);
            var snapshot = loaded.Entities.Single(entity => entity.ActorId == "player");
            Assert.NotNull(snapshot.Inventory);
            Assert.Equal("rusty_sword", snapshot.Inventory.EquippedWeaponId);
            Assert.Single(snapshot.Inventory.Stacks);
            Assert.Equal(3, snapshot.Inventory.Stacks[0].Count);
        }
        finally
        {
            if (File.Exists(savePath)) File.Delete(savePath);
        }
    }

    private static Entity CreatePlayer(World world, Position position)
    {
        var player = new Entity();
        player.AddComponent(new ActorIdComponent("player"));
        player.AddComponent(new PositionComponent(position));
        player.AddComponent(new RenderableComponent('@', new RgbColor(255, 255, 255)));
        player.AddComponent(new HealthComponent(10));
        player.AddComponent(new IsPlayerComponent());
        player.AddComponent(new InventoryComponent());
        world.AddEntity(player);
        return player;
    }
}
