using RogueEngine.Engine.Commands;
using RogueEngine.Engine.Core;
using SadConsole.Input;

namespace RogueEngine.SadConsole.Input;

public static class InputHandler
{
    public static ICommand? GetMoveCommand(Keyboard keyboard, Entity player)
    {
        ArgumentNullException.ThrowIfNull(keyboard);
        ArgumentNullException.ThrowIfNull(player);

        if (keyboard.IsKeyPressed(Keys.Up) || keyboard.IsKeyPressed(Keys.W))
        {
            return new MoveCommand(player, 0, -1);
        }

        if (keyboard.IsKeyPressed(Keys.Down) || keyboard.IsKeyPressed(Keys.S))
        {
            return new MoveCommand(player, 0, 1);
        }

        if (keyboard.IsKeyPressed(Keys.Left) || keyboard.IsKeyPressed(Keys.A))
        {
            return new MoveCommand(player, -1, 0);
        }

        if (keyboard.IsKeyPressed(Keys.Right) || keyboard.IsKeyPressed(Keys.D))
        {
            return new MoveCommand(player, 1, 0);
        }

        return null;
    }
}
