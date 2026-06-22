using RogueEngine.Engine.Combat;
using RogueEngine.Engine.Core;

namespace RogueEngine.Engine.Commands;

public sealed class AttackCommand : ICommand
{
    public Entity Attacker { get; }
    public Entity Target { get; }
    public int? DamageOverride { get; }

    public AttackCommand(Entity attacker, Entity target, int? damageOverride = null)
    {
        Attacker = attacker ?? throw new ArgumentNullException(nameof(attacker));
        Target = target ?? throw new ArgumentNullException(nameof(target));
        DamageOverride = damageOverride;

        if (damageOverride is <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(damageOverride));
        }
    }

    public bool Execute(World world)
    {
        ArgumentNullException.ThrowIfNull(world);
        return CombatSystem.Attack(world, Attacker, Target, DamageOverride);
    }
}
