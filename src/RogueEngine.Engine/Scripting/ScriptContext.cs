using RogueEngine.Engine.Commands;
using RogueEngine.Engine.Components;
using RogueEngine.Engine.Core;

namespace RogueEngine.Engine.Scripting;

public sealed class ScriptContext : IScriptContext
{
    private readonly World _world;
    private readonly Entity _entity;

    public ScriptContext(World world, Entity entity)
    {
        _world = world ?? throw new ArgumentNullException(nameof(world));
        _entity = entity ?? throw new ArgumentNullException(nameof(entity));
    }

    public Position Position
    {
        get
        {
            if (!_entity.TryGetComponent<PositionComponent>(out var position) || position is null)
            {
                throw new InvalidOperationException("Script entity is missing PositionComponent.");
            }

            return position.Position;
        }
    }

    public int CurrentHp
    {
        get
        {
            if (!_entity.TryGetComponent<HealthComponent>(out var health) || health is null)
            {
                throw new InvalidOperationException("Script entity is missing HealthComponent.");
            }

            return health.CurrentHp;
        }
    }

    public Position? FindPlayer()
    {
        var player = _world.GetPlayer();
        if (player is null)
        {
            return null;
        }

        if (!player.TryGetComponent<PositionComponent>(out var position) || position is null)
        {
            return null;
        }

        return position.Position;
    }

    public bool MoveToward(Position target)
    {
        var current = Position;
        var deltaX = target.X - current.X;
        var deltaY = target.Y - current.Y;

        if (deltaX == 0 && deltaY == 0)
        {
            return false;
        }

        var stepX = Math.Sign(deltaX);
        var stepY = Math.Sign(deltaY);

        if (Math.Abs(deltaX) >= Math.Abs(deltaY))
        {
            if (TryMove(stepX, 0))
            {
                return true;
            }

            return TryMove(0, stepY);
        }

        if (TryMove(0, stepY))
        {
            return true;
        }

        return TryMove(stepX, 0);
    }

    public bool AttackAt(Position target)
    {
        var targetEntity = _world.GetEntityAt(target);
        if (targetEntity is null)
        {
            return false;
        }

        return new AttackCommand(_entity, targetEntity).Execute(_world);
    }

    public void Log(string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        _world.Log.Add(message);
    }

    private bool TryMove(int deltaX, int deltaY)
    {
        if (deltaX == 0 && deltaY == 0)
        {
            return false;
        }

        return new MoveCommand(_entity, deltaX, deltaY).Execute(_world);
    }
}
