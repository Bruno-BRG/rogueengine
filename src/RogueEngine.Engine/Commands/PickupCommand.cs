using RogueEngine.Engine.Components;
using RogueEngine.Engine.Core;
using RogueEngine.Engine.Rules;
using RogueEngine.Engine.Data;

namespace RogueEngine.Engine.Commands;

public sealed class PickupCommand : ICommand
{
    public Entity Entity { get; }

    public PickupCommand(Entity entity) => Entity = entity ?? throw new ArgumentNullException(nameof(entity));

    public bool Execute(World world)
    {
        ArgumentNullException.ThrowIfNull(world);

        if (!Entity.TryGetComponent<PositionComponent>(out var position) || position is null)
        {
            return false;
        }

        if (!Entity.TryGetComponent<InventoryComponent>(out var inventory) || inventory is null)
        {
            return false;
        }

        var pickup = FindPickupTarget(world, position.Position);
        if (pickup is null ||
            !pickup.TryGetComponent<ItemPickupComponent>(out var itemPickup) ||
            itemPickup is null)
        {
            world.Log.Add("Nothing to pick up.");
            return false;
        }

        AddToInventory(inventory, itemPickup.ItemId, itemPickup.Count);
        world.RemoveEntity(pickup);
        world.Log.Add($"Picked up {itemPickup.ItemId} x{itemPickup.Count}.");
        world.Raise(new ItemPickedUpEvent(Entity, itemPickup.ItemId, itemPickup.Count));
        return true;
    }

    private static Entity? FindPickupTarget(World world, Position playerPosition)
    {
        if (world.GetEntityAt(playerPosition) is { } atPlayer &&
            atPlayer.HasComponent<ItemPickupComponent>())
        {
            return atPlayer;
        }

        foreach (var neighbor in GetAdjacent(playerPosition))
        {
            var entity = world.GetEntityAt(neighbor);
            if (entity is not null && entity.HasComponent<ItemPickupComponent>())
            {
                return entity;
            }
        }

        return null;
    }

    private static IEnumerable<Position> GetAdjacent(Position position)
    {
        yield return new Position(position.X + 1, position.Y);
        yield return new Position(position.X - 1, position.Y);
        yield return new Position(position.X, position.Y + 1);
        yield return new Position(position.X, position.Y - 1);
    }

    public static void AddToInventory(InventoryComponent inventory, string itemId, int count)
    {
        foreach (var stack in inventory.Stacks)
        {
            if (stack.ItemId == itemId)
            {
                stack.Count += count;
                return;
            }
        }

        inventory.Stacks.Add(new InventoryStack { ItemId = itemId, Count = count });
    }
}
