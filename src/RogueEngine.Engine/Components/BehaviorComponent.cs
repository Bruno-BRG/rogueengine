using RogueEngine.Engine.Core;
using RogueEngine.Engine.Scripting;
using RogueEngine.Engine.TurnBased;

namespace RogueEngine.Engine.Components;

public sealed class BehaviorComponent : IComponent, ITurnActor
{
    public Entity Entity { get; }
    public IBehavior Behavior { get; }

    public BehaviorComponent(Entity entity, IBehavior behavior)
    {
        Entity = entity ?? throw new ArgumentNullException(nameof(entity));
        Behavior = behavior ?? throw new ArgumentNullException(nameof(behavior));
    }

    public void TakeTurn(World world)
    {
        ArgumentNullException.ThrowIfNull(world);

        if (!Entity.TryGetComponent<HealthComponent>(out var health) || health is null || !health.IsAlive)
        {
            return;
        }

        var context = new ScriptContext(world, Entity);
        Behavior.OnTurn(context);
    }
}
