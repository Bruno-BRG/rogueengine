using RogueEngine.Engine.Commands;
using RogueEngine.Engine.Components;
using RogueEngine.Engine.Core;
using RogueEngine.Engine.Data;
using RogueEngine.Engine.Scripting;

namespace RogueEngine.Engine.Rules;

public sealed class ItemEffectRegistry
{
    private readonly ScriptAssembly? _scripts;

    public ItemEffectRegistry(ScriptAssembly? scripts) => _scripts = scripts;

    public bool Apply(World world, Entity entity, ItemDefinition item, IReadOnlyDictionary<string, ItemDefinition> items)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(entity);
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(items);

        var context = new ItemEffectContext(world, entity, item, items);

        if (!string.IsNullOrWhiteSpace(item.OnUse.Script))
        {
            var script = _scripts?.CreateItemEffect(item.OnUse.Script);
            if (script is null)
            {
                world.Log.Add($"Unknown item effect script '{item.OnUse.Script}'.");
                return false;
            }

            return script.Apply(context);
        }

        var effect = ResolveEffectName(item);
        return effect switch
        {
            "heal" => ApplyHeal(context, ResolveAmount(item)),
            "damage" => ApplyDamage(context, ResolveAmount(item)),
            "buff" => ApplyBuff(context),
            _ => false
        };
    }

    private static string ResolveEffectName(ItemDefinition item)
    {
        if (!string.IsNullOrWhiteSpace(item.OnUse.Effect))
        {
            return item.OnUse.Effect;
        }

        if (item.OnUse.Heal > 0)
        {
            return "heal";
        }

        return string.Empty;
    }

    private static int ResolveAmount(ItemDefinition item) =>
        item.OnUse.Amount > 0 ? item.OnUse.Amount : item.OnUse.Heal;

    private static bool ApplyHeal(ItemEffectContext context, int amount)
    {
        if (amount <= 0 ||
            !context.Entity.TryGetComponent<HealthComponent>(out var health) ||
            health is null)
        {
            return false;
        }

        health.Heal(amount);
        context.Log($"You recover {amount} HP.");
        return true;
    }

    private static bool ApplyDamage(ItemEffectContext context, int amount)
    {
        if (amount <= 0)
        {
            return false;
        }

        var target = FindAdjacentEnemy(context.World, context.Entity);
        if (target is null)
        {
            context.Log("No target nearby.");
            return false;
        }

        return Combat.CombatSystem.Attack(context.World, context.Entity, target, amount);
    }

    private static bool ApplyBuff(ItemEffectContext context)
    {
        context.Log($"You feel empowered by {context.Item.Name}.");
        return true;
    }

    private static Entity? FindAdjacentEnemy(World world, Entity source)
    {
        if (!source.TryGetComponent<PositionComponent>(out var position) || position is null)
        {
            return null;
        }

        foreach (var neighbor in GetAdjacent(position.Position))
        {
            var entity = world.GetEntityAt(neighbor);
            if (entity is not null &&
                entity.Id != source.Id &&
                !entity.HasComponent<IsPlayerComponent>() &&
                entity.TryGetComponent<HealthComponent>(out var health) &&
                health is not null &&
                health.IsAlive)
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
}
