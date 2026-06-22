using RogueEngine.Engine.Components;
using RogueEngine.Engine.Core;
using RogueEngine.Engine.Data;

namespace RogueEngine.Engine.Rules;

public static class StatResolver
{
    public static int GetAttackDamage(Entity attacker, GameRulesContext rules)
    {
        ArgumentNullException.ThrowIfNull(attacker);
        ArgumentNullException.ThrowIfNull(rules);

        var damage = Combat.CombatSystem.DefaultAttackDamage;

        if (attacker.TryGetComponent<ActorIdComponent>(out var actorId) &&
            actorId is not null &&
            rules.Project.Actors.TryGetValue(actorId.ActorId, out _))
        {
            damage = Combat.CombatSystem.DefaultAttackDamage;
        }

        if (attacker.TryGetComponent<ClassComponent>(out var classComponent) &&
            classComponent is not null &&
            rules.Project.Classes.TryGetValue(classComponent.ClassId, out var classDef))
        {
            damage += classDef.StatBonuses.Attack;
        }

        if (attacker.TryGetComponent<InventoryComponent>(out var inventory) && inventory is not null)
        {
            if (!string.IsNullOrWhiteSpace(inventory.EquippedWeaponId) &&
                rules.Project.Items.TryGetValue(inventory.EquippedWeaponId, out var weapon))
            {
                damage += weapon.Stats.Attack;
            }
        }

        return Math.Max(1, damage);
    }

    public static int GetDefense(Entity entity, GameRulesContext rules)
    {
        ArgumentNullException.ThrowIfNull(entity);
        ArgumentNullException.ThrowIfNull(rules);

        var defense = 0;

        if (entity.TryGetComponent<ClassComponent>(out var classComponent) &&
            classComponent is not null &&
            rules.Project.Classes.TryGetValue(classComponent.ClassId, out var classDef))
        {
            defense += classDef.StatBonuses.Defense;
        }

        if (entity.TryGetComponent<InventoryComponent>(out var inventory) && inventory is not null)
        {
            if (!string.IsNullOrWhiteSpace(inventory.EquippedArmorId) &&
                rules.Project.Items.TryGetValue(inventory.EquippedArmorId, out var armor))
            {
                defense += armor.Stats.Defense;
            }
        }

        return defense;
    }

    public static int GetMaxHp(Entity entity, ActorDefinition actor, GameRulesContext? rules)
    {
        ArgumentNullException.ThrowIfNull(entity);
        ArgumentNullException.ThrowIfNull(actor);

        var maxHp = actor.MaxHp;

        if (rules is not null &&
            entity.TryGetComponent<ClassComponent>(out var classComponent) &&
            classComponent is not null &&
            rules.Project.Classes.TryGetValue(classComponent.ClassId, out var classDef))
        {
            maxHp = classDef.BaseMaxHp > 0 ? classDef.BaseMaxHp : maxHp;
            maxHp += classDef.StatBonuses.MaxHp;
        }

        return Math.Max(1, maxHp);
    }
}
