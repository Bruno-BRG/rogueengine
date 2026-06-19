using RogueEngine.Engine.Commands;
using RogueEngine.Engine.Components;
using RogueEngine.Engine.Core;

namespace RogueEngine.Engine.AI;

public static class ChaseAI
{
    public static void TakeTurn(World world, Entity enemy)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(enemy);

        var player = world.GetPlayer();
        if (player is null)
        {
            return;
        }

        if (!enemy.TryGetComponent<HealthComponent>(out var enemyHealth) || enemyHealth is null || !enemyHealth.IsAlive)
        {
            return;
        }

        if (!enemy.TryGetComponent<PositionComponent>(out var enemyPosition) || enemyPosition is null)
        {
            return;
        }

        if (!player.TryGetComponent<PositionComponent>(out var playerPosition) || playerPosition is null)
        {
            return;
        }

        if (!player.TryGetComponent<HealthComponent>(out var playerHealth) || playerHealth is null || !playerHealth.IsAlive)
        {
            return;
        }

        var deltaX = playerPosition.Position.X - enemyPosition.Position.X;
        var deltaY = playerPosition.Position.Y - enemyPosition.Position.Y;

        if (Math.Abs(deltaX) <= 1 && Math.Abs(deltaY) <= 1 && (deltaX != 0 || deltaY != 0))
        {
            new AttackCommand(enemy, player).Execute(world);
            return;
        }

        var stepX = Math.Sign(deltaX);
        var stepY = Math.Sign(deltaY);

        if (Math.Abs(deltaX) >= Math.Abs(deltaY))
        {
            if (TryMove(world, enemy, stepX, 0))
            {
                return;
            }

            TryMove(world, enemy, 0, stepY);
            return;
        }

        if (TryMove(world, enemy, 0, stepY))
        {
            return;
        }

        TryMove(world, enemy, stepX, 0);
    }

    private static bool TryMove(World world, Entity entity, int deltaX, int deltaY)
    {
        if (deltaX == 0 && deltaY == 0)
        {
            return false;
        }

        return new MoveCommand(entity, deltaX, deltaY).Execute(world);
    }
}
