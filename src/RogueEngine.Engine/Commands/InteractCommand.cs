using RogueEngine.Engine.Components;
using RogueEngine.Engine.Core;
using RogueEngine.Engine.Data;
using RogueEngine.Engine.Rules;

namespace RogueEngine.Engine.Commands;

public sealed class InteractCommand : ICommand
{
    public Entity Entity { get; }

    public InteractCommand(Entity entity) => Entity = entity ?? throw new ArgumentNullException(nameof(entity));

    public bool Execute(World world)
    {
        ArgumentNullException.ThrowIfNull(world);

        if (world.Rules is null ||
            !Entity.TryGetComponent<PositionComponent>(out var position) ||
            position is null)
        {
            world.Log.Add("Nothing to interact with.");
            return false;
        }

        var target = FindInteractionTarget(world, position.Position);
        if (target is null)
        {
            world.Log.Add("Nothing to interact with.");
            return false;
        }

        if (!target.TryGetComponent<InteractionComponent>(out var interactionComponent) ||
            interactionComponent is null ||
            interactionComponent.IsConsumed)
        {
            return false;
        }

        if (!target.TryGetComponent<PositionComponent>(out var targetPosition) ||
            targetPosition is null ||
            !world.Rules.Project.Interactions.TryGetValue(interactionComponent.InteractionId, out var definition))
        {
            world.Log.Add("Unknown interaction.");
            return false;
        }

        return world.Rules.Interactions.TryInteract(
            world,
            Entity,
            definition,
            targetPosition.Position,
            world.Rules.Project.Items);
    }

    private static Entity? FindInteractionTarget(World world, Position playerPosition)
    {
        var atPlayer = world.GetEntityAt(playerPosition);
        if (atPlayer?.HasComponent<InteractionComponent>() == true)
        {
            return atPlayer;
        }

        foreach (var neighbor in GetAdjacent(playerPosition))
        {
            var entity = world.GetEntityAt(neighbor);
            if (entity?.HasComponent<InteractionComponent>() == true)
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
