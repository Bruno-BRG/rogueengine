using RogueEngine.Engine.Components;

namespace RogueEngine.Engine.Core;

public sealed class World
{
    public TileMap Map { get; }
    public MessageLog Log { get; } = new();

    private readonly List<Entity> _entities = [];

    public IReadOnlyList<Entity> Entities => _entities;

    public World(TileMap map)
    {
        ArgumentNullException.ThrowIfNull(map);
        Map = map;
    }

    public Entity AddEntity(Entity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        _entities.Add(entity);
        return entity;
    }

    public bool RemoveEntity(Entity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        return _entities.Remove(entity);
    }

    public Entity? GetPlayer()
    {
        foreach (var entity in _entities)
        {
            if (entity.HasComponent<IsPlayerComponent>())
            {
                return entity;
            }
        }

        return null;
    }

    public Entity? GetEntityAt(Position position)
    {
        foreach (var entity in _entities)
        {
            if (!entity.TryGetComponent<PositionComponent>(out var positionComponent) || positionComponent is null)
            {
                continue;
            }

            if (positionComponent.Position == position)
            {
                return entity;
            }
        }

        return null;
    }

    public bool CanMoveTo(Position position, Entity movingEntity)
    {
        if (!Map.IsWalkable(position))
        {
            return false;
        }

        var occupant = GetEntityAt(position);
        if (occupant is null || occupant.Id == movingEntity.Id)
        {
            return true;
        }

        return !occupant.HasComponent<BlocksMovementComponent>();
    }

    public bool MoveEntity(Entity entity, Position newPosition)
    {
        if (!entity.TryGetComponent<PositionComponent>(out var positionComponent) || positionComponent is null)
        {
            return false;
        }

        if (!CanMoveTo(newPosition, entity))
        {
            return false;
        }

        positionComponent.Position = newPosition;
        return true;
    }
}
