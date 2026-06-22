using RogueEngine.Engine.Components;
using RogueEngine.Engine.Core;
using RogueEngine.Engine.Data;

namespace RogueEngine.Engine.Commands;

public sealed class UseItemCommand : ICommand
{
    public Entity Entity { get; }
    public int InventorySlot { get; }

    public UseItemCommand(Entity entity, int inventorySlot)
    {
        Entity = entity ?? throw new ArgumentNullException(nameof(entity));
        InventorySlot = inventorySlot;
    }

    public bool Execute(World world, IReadOnlyDictionary<string, ItemDefinition> items)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(items);

        if (!Entity.TryGetComponent<InventoryComponent>(out var inventory) || inventory is null)
        {
            return false;
        }

        if (InventorySlot < 1 || InventorySlot > inventory.Stacks.Count)
        {
            world.Log.Add("Invalid inventory slot.");
            return false;
        }

        var stack = inventory.Stacks[InventorySlot - 1];
        if (!items.TryGetValue(stack.ItemId, out var definition))
        {
            world.Log.Add($"Unknown item '{stack.ItemId}'.");
            return false;
        }

        if (!string.Equals(definition.Kind, "consumable", StringComparison.OrdinalIgnoreCase))
        {
            world.Log.Add($"{definition.Name} is not consumable.");
            return false;
        }

        if (definition.OnUse.Heal > 0 &&
            Entity.TryGetComponent<HealthComponent>(out var health) &&
            health is not null)
        {
            health.Heal(definition.OnUse.Heal);
            world.Log.Add($"You drink {definition.Name} and recover {definition.OnUse.Heal} HP.");
        }
        else
        {
            world.Log.Add($"You use {definition.Name}.");
        }

        stack.Count--;
        if (stack.Count <= 0)
        {
            inventory.Stacks.RemoveAt(InventorySlot - 1);
        }

        return true;
    }

    public bool Execute(World world) => Execute(world, new Dictionary<string, ItemDefinition>());
}
