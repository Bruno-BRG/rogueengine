using RogueEngine.Engine.Core;

namespace RogueEngine.Engine.Components;

public sealed class HealthComponent : IComponent
{
    public int MaxHp { get; private set; }
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

    public void SetMaxHp(int maxHp)
    {
        if (maxHp <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxHp));
        }

        MaxHp = maxHp;
        CurrentHp = Math.Min(CurrentHp, MaxHp);
    }

    public void TakeDamage(int amount)
    {
        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount));
        }

        CurrentHp = Math.Max(0, CurrentHp - amount);
    }

    public void Heal(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        CurrentHp = Math.Min(MaxHp, CurrentHp + amount);
    }
}
