using RogueEngine.Engine.Core;

namespace RogueEngine.Engine.Components;

public sealed class HealthComponent : IComponent
{
    public int MaxHp { get; }
    public int CurrentHp { get; private set; }

    public bool IsAlive => CurrentHp > 0;

    public HealthComponent(int maxHp, int? currentHp = null)
    {
        if (maxHp <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxHp));
        }

        MaxHp = maxHp;
        CurrentHp = currentHp is null ? maxHp : Math.Clamp(currentHp.Value, 0, maxHp);
    }

    public void TakeDamage(int amount)
    {
        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount));
        }

        CurrentHp = Math.Max(0, CurrentHp - amount);
    }
}
