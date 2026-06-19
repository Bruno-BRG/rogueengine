using RogueEngine.Engine.Combat;
using RogueEngine.Engine.Core;

namespace RogueEngine.Engine.Commands;

public sealed class AttackCommand : ICommand
{
    public Entity Attacker { get; }
    public Entity Target { get; }
    public int Damage { get; }

    public AttackCommand(Entity attacker, Entity target, int damage = CombatSystem.DefaultAttackDamage)
    {
        Attacker = attacker ?? throw new ArgumentNullException(nameof(attacker));
        Target = target ?? throw new ArgumentNullException(nameof(target));

        if (damage <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(damage));
        }

        Damage = damage;
    }

    public bool Execute(World world)
    {
        ArgumentNullException.ThrowIfNull(world);
        return CombatSystem.Attack(world, Attacker, Target, Damage);
    }
}
