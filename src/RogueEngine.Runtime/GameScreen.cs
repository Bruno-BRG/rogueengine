using RogueEngine.Engine.Components;
using RogueEngine.Engine.Core;
using RogueEngine.Engine.Data;
using RogueEngine.Engine.Rendering;
using RogueEngine.Engine.TurnBased;
using RogueEngine.SadConsole.Input;
using RogueEngine.SadConsole.Rendering;
using SadConsole;
using SadConsole.Input;

namespace RogueEngine.Runtime;

internal sealed class GameScreen : ScreenObject
{
    private LoadedProject _project;
    private World _world;
    private Entity _player;
    private TurnManager _turnManager;
    private int _seed;
    private readonly ScreenSurface _mapSurface;
    private readonly IRenderer _mapRenderer;
    private readonly MessagePanel _messagePanel;

    public GameScreen()
    {
        _project = RuntimeBootstrap.Project;

        var initialSetup = WorldBuilder.CreateNewGame(_project, RuntimeBootstrap.Scripts);
        _world = initialSetup.World;
        _player = initialSetup.Player;
        _turnManager = initialSetup.TurnManager;
        _seed = initialSetup.Seed;

        _mapSurface = new ScreenSurface(_project.Settings.MapWidth, _project.Settings.MapHeight)
        {
            UseMouse = false,
            Position = new Point(0, 0)
        };

        _mapRenderer = new SadConsoleMapRenderer(_mapSurface);
        _messagePanel = new MessagePanel(
            _project.Settings.MapWidth,
            _project.Settings.MessagePanelHeight,
            _project.Settings.MapHeight);

        Children.Add(_mapSurface);
        Children.Add(_messagePanel.Surface);

        RenderAll();
    }

    public override bool ProcessKeyboard(Keyboard keyboard)
    {
        if (keyboard.IsKeyPressed(Keys.F9))
        {
            TryLoadGame();
            return true;
        }

        if (keyboard.IsKeyPressed(Keys.F5))
        {
            SaveGame();
            return true;
        }

        if (!IsPlayerAlive())
        {
            return base.ProcessKeyboard(keyboard);
        }

        var command = InputHandler.GetMoveCommand(keyboard, _player);
        if (command is not null)
        {
            var acted = command.Execute(_world);
            if (acted)
            {
                _turnManager.RunEnemyTurns(_world);
            }

            RenderAll();
            return true;
        }

        return base.ProcessKeyboard(keyboard);
    }

    private void SaveGame()
    {
        GameSaveLoad.Save(_world, _seed, _project.SaveFilePath);
        _world.Log.Add("Game saved.");
        RenderAll();
    }

    private void TryLoadGame()
    {
        if (!GameSaveLoad.TryLoadSaveData(_project.SaveFilePath, out var saveData) || saveData is null)
        {
            _world.Log.Add("No save file found.");
            RenderAll();
            return;
        }

        ApplySetup(WorldBuilder.CreateFromSave(_project, saveData, RuntimeBootstrap.Scripts));
        RenderAll();
    }

    private void ApplySetup(GameSetup setup)
    {
        _project = setup.Project;
        _world = setup.World;
        _player = setup.Player;
        _turnManager = setup.TurnManager;
        _seed = setup.Seed;
    }

    private void RenderAll()
    {
        _mapRenderer.Render(_world);
        _messagePanel.Render(_world.Log);
    }

    private bool IsPlayerAlive()
    {
        if (!_player.TryGetComponent<HealthComponent>(out var health) || health is null)
        {
            return true;
        }

        return health.IsAlive;
    }
}
