using RogueEngine.Engine.Components;
using RogueEngine.Engine.Core;

namespace RogueEngine.Engine.Commands;

public sealed class MoveCommand : ICommand
{
    public Entity Entity { get; }
    public int DeltaX { get; }
    public int DeltaY { get; }

    public MoveCommand(Entity entity, int deltaX, int deltaY)
    {
        Entity = entity ?? throw new ArgumentNullException(nameof(entity));
        DeltaX = deltaX;
        DeltaY = deltaY;
    }

    public bool Execute(World world)
    {
        ArgumentNullException.ThrowIfNull(world);

        if (!Entity.TryGetComponent<PositionComponent>(out var positionComponent) || positionComponent is null)
        {
            return false;
        }

        var target = positionComponent.Position + new Position(DeltaX, DeltaY);
        var occupant = world.GetEntityAt(target);
        if (occupant is not null &&
            occupant.Id != Entity.Id &&
            occupant.TryGetComponent<HealthComponent>(out var health) &&
            health is not null &&
            health.IsAlive)
        {
            return new AttackCommand(Entity, occupant).Execute(world);
        }

        return world.MoveEntity(Entity, target);
    }
}
