using RogueEngine.Engine.Components;
using RogueEngine.Engine.Core;
using RogueEngine.Engine.Data;
using RogueEngine.Engine.Scripting;

namespace RogueEngine.Engine.Rules;

public sealed class InteractionRegistry
{
    private readonly ScriptAssembly? _scripts;

    public InteractionRegistry(ScriptAssembly? scripts) => _scripts = scripts;

    public bool TryInteract(
        World world,
        Entity entity,
        InteractionDefinition interaction,
        Position position,
        IReadOnlyDictionary<string, ItemDefinition> items)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(entity);
        ArgumentNullException.ThrowIfNull(interaction);
        ArgumentNullException.ThrowIfNull(items);

        var context = new InteractionContext(world, entity, interaction, position, items);

        if (!string.IsNullOrWhiteSpace(interaction.Script))
        {
            var handler = _scripts?.CreateInteractionHandler(interaction.Script);
            if (handler is null)
            {
                world.Log.Add($"Unknown interaction script '{interaction.Script}'.");
                return false;
            }

            return handler.TryInteract(context);
        }

        return interaction.Kind.ToLowerInvariant() switch
        {
            "door" => TryOpenDoor(context),
            "stairs" => TryUseStairs(context),
            "use" => TryUseMessage(context),
            _ => false
        };
    }

    private static bool TryOpenDoor(InteractionContext context)
    {
        if (!string.IsNullOrWhiteSpace(context.Interaction.RequiredKeyId) &&
            !HasKey(context.Entity, context.Interaction.RequiredKeyId, context.Items))
        {
            context.Log("The door is locked.");
            return false;
        }

        var interactionEntity = context.World.GetEntityAt(context.Position);
        if (interactionEntity?.TryGetComponent<InteractionComponent>(out var component) == true &&
            component is not null)
        {
            component.IsConsumed = true;
            context.World.RemoveEntity(interactionEntity);
        }

        context.Log(string.IsNullOrWhiteSpace(context.Interaction.Message)
            ? "The door opens."
            : context.Interaction.Message);
        context.World.Raise(new InteractionTriggeredEvent(context.Entity, context.Interaction.Id, context.Position));
        return true;
    }

    private static bool TryUseStairs(InteractionContext context)
    {
        context.Log(string.IsNullOrWhiteSpace(context.Interaction.Message)
            ? $"Stairs lead to {context.Interaction.TargetScene ?? "another place"}."
            : context.Interaction.Message);
        context.World.Raise(new InteractionTriggeredEvent(context.Entity, context.Interaction.Id, context.Position));
        return true;
    }

    private static bool TryUseMessage(InteractionContext context)
    {
        context.Log(context.Interaction.Message ?? "Nothing happens.");
        context.World.Raise(new InteractionTriggeredEvent(context.Entity, context.Interaction.Id, context.Position));
        return true;
    }

    internal static bool HasKey(Entity entity, string requiredKeyId, IReadOnlyDictionary<string, ItemDefinition> items)
    {
        if (!entity.TryGetComponent<InventoryComponent>(out var inventory) || inventory is null)
        {
            return false;
        }

        foreach (var stack in inventory.Stacks)
        {
            if (!items.TryGetValue(stack.ItemId, out var item))
            {
                continue;
            }

            if (string.Equals(item.Kind, "key", StringComparison.OrdinalIgnoreCase) &&
                string.Equals(item.KeyId ?? item.Id, requiredKeyId, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
