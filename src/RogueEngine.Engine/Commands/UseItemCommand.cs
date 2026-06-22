using RogueEngine.Engine.Components;
using RogueEngine.Engine.Core;
using RogueEngine.Engine.Data;
using RogueEngine.Engine.Rules;

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

    public bool Execute(World world)
    {
        ArgumentNullException.ThrowIfNull(world);

        if (world.Rules is null)
        {
            return false;
        }

        return Execute(world, world.Rules.Project.Items);
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

        var rules = world.Rules;
        if (rules is null)
        {
            world.Log.Add("Cannot use items right now.");
            return false;
        }

        if (!rules.ItemEffects.Apply(world, Entity, definition, items))
        {
            world.Log.Add($"You use {definition.Name}.");
        }

        stack.Count--;
        if (stack.Count <= 0)
        {
            inventory.Stacks.RemoveAt(InventorySlot - 1);
        }

        world.Raise(new ItemUsedEvent(Entity, definition.Id));
        return true;
    }
}
