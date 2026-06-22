using RogueEngine.Engine.Components;
using RogueEngine.Engine.Core;
using RogueEngine.Engine.Rules;

namespace RogueEngine.Engine.Combat;

public static class CombatSystem
{
    public const int DefaultAttackDamage = 3;

    public static bool Attack(World world, Entity attacker, Entity target, int? damageOverride = null)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(attacker);
        ArgumentNullException.ThrowIfNull(target);

        if (!target.TryGetComponent<HealthComponent>(out var health) || health is null || !health.IsAlive)
        {
            return false;
        }

        var damage = damageOverride ?? (world.Rules is not null
            ? StatResolver.GetAttackDamage(attacker, world.Rules)
            : DefaultAttackDamage);

        if (world.Rules is not null)
        {
            damage = Math.Max(1, damage - StatResolver.GetDefense(target, world.Rules));
        }

        health.TakeDamage(damage);
        world.Log.Add($"{GetEntityName(attacker)} hits {GetEntityName(target)} for {damage} damage.");

        if (!health.IsAlive)
        {
            world.RemoveEntity(target);
            world.Log.Add($"{GetEntityName(target)} dies.");
            world.Raise(new EntityKilledEvent(target, attacker));
        }

        return true;
    }

    private static string GetEntityName(Entity entity)
    {
        if (entity.HasComponent<IsPlayerComponent>())
        {
            return "You";
        }

        if (entity.TryGetComponent<RenderableComponent>(out var renderable) && renderable is not null)
        {
            return $"the {renderable.Glyph}";
        }

        return "something";
    }
}
