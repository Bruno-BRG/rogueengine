using RogueEngine.Engine.AI;
using RogueEngine.Engine.Core;
using RogueEngine.Engine.TurnBased;

namespace RogueEngine.Engine.Components;

public sealed class EnemyTurnComponent : IComponent, ITurnActor
{
    public Entity Entity { get; }

    public EnemyTurnComponent(Entity entity)
    {
        Entity = entity ?? throw new ArgumentNullException(nameof(entity));
    }

    public void TakeTurn(World world)
    {
        ChaseAI.TakeTurn(world, Entity);
    }
}
