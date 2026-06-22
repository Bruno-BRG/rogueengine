using RogueEngine.Engine.Core;
using RogueEngine.Engine.Scripting;

public sealed class GoblinBehavior : IBehavior
{
    public void OnTurn(IScriptContext context)
    {
        var playerPosition = context.FindPlayer();
        if (playerPosition is null)
        {
            return;
        }

        if (IsAdjacent(context.Position, playerPosition.Value))
        {
            context.AttackAt(playerPosition.Value);
            return;
        }

        context.MoveToward(playerPosition.Value);
    }

    private static bool IsAdjacent(Position left, Position right)
    {
        var deltaX = Math.Abs(left.X - right.X);
        var deltaY = Math.Abs(left.Y - right.Y);
        return deltaX <= 1 && deltaY <= 1 && (deltaX != 0 || deltaY != 0);
    }
}
