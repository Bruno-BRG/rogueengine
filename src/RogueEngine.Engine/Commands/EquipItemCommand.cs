using RogueEngine.Engine.Components;
using RogueEngine.Engine.Core;
using RogueEngine.Engine.Data;

namespace RogueEngine.Engine.Commands;

public sealed class EquipItemCommand : ICommand
{
    public Entity Entity { get; }
    public int InventorySlot { get; }

    public EquipItemCommand(Entity entity, int inventorySlot)
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

        if (!string.Equals(definition.Kind, "equipment", StringComparison.OrdinalIgnoreCase))
        {
            world.Log.Add($"{definition.Name} is not equipment.");
            return false;
        }

        if (string.Equals(definition.EquipSlot, "weapon", StringComparison.OrdinalIgnoreCase))
        {
            inventory.EquippedWeaponId = definition.Id;
            world.Log.Add($"Equipped {definition.Name} as weapon.");
            return true;
        }

        if (string.Equals(definition.EquipSlot, "armor", StringComparison.OrdinalIgnoreCase))
        {
            inventory.EquippedArmorId = definition.Id;
            world.Log.Add($"Equipped {definition.Name} as armor.");
            return true;
        }

        world.Log.Add($"{definition.Name} has no equip slot.");
        return false;
    }

    public bool Execute(World world) => Execute(world, new Dictionary<string, ItemDefinition>());
}
