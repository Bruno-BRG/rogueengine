using RogueEngine.Engine.Commands;
using RogueEngine.Engine.Components;
using RogueEngine.Engine.Core;
using RogueEngine.Engine.Data;
using RogueEngine.Engine.Rendering;
using RogueEngine.Engine.TurnBased;
using RogueEngine.SadConsole.Input;
using RogueEngine.SadConsole.Rendering;
using RogueEngine.Toolkit.Fov;
using RogueEngine.Toolkit.Overworld;
using SadConsole;
using SadConsole.Input;

namespace RogueEngine.Runtime;

internal sealed class GameScreen : ScreenObject
{
    private const int FovRadius = 10;

    private enum GameMode
    {
        Overworld,
        Dungeon
    }

    private LoadedProject _project;
    private World _world;
    private Entity _player;
    private TurnManager _turnManager;
    private int _seed;
    private GameMode _mode;
    private OverworldService? _overworldService;
    private readonly ScreenSurface _mapSurface;
    private readonly IRenderer _mapRenderer;
    private readonly MessagePanel _messagePanel;

    public GameScreen()
    {
        _project = RuntimeBootstrap.Project;
        var initialSetup = CreateInitialSetup();
        ApplySetup(initialSetup);

        _mapSurface = new ScreenSurface(_world.Map.Width, _world.Map.Height)
        {
            UseMouse = false,
            Position = new Point(0, 0)
        };

        _mapRenderer = new SadConsoleMapRenderer(_mapSurface);
        _messagePanel = new MessagePanel(
            _world.Map.Width,
            _project.Settings.MessagePanelHeight,
            _world.Map.Height);

        Children.Add(_mapSurface);
        Children.Add(_messagePanel.Surface);

        if (_mode == GameMode.Dungeon)
        {
            UpdateFieldOfView();
        }

        RenderAll();
    }

    private GameSetup CreateInitialSetup()
    {
        if (_project.DefaultOverworld is not null &&
            string.IsNullOrWhiteSpace(_project.Project.DefaultScene))
        {
            _mode = GameMode.Overworld;
            _overworldService = new OverworldService(_project.DefaultOverworld);
            return WorldBuilder.CreateOverworldGame(_project, RuntimeBootstrap.Scripts);
        }

        _mode = GameMode.Dungeon;
        return WorldBuilder.CreateNewGame(_project, RuntimeBootstrap.Scripts);
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

        if (TryHandleItemInput(keyboard))
        {
            RenderAll();
            return true;
        }

        if (_mode == GameMode.Overworld && keyboard.IsKeyPressed(Keys.Enter))
        {
            TryEnterOverworldCell();
            RenderAll();
            return true;
        }

        if (keyboard.IsKeyPressed(Keys.Space) && _mode == GameMode.Dungeon)
        {
            if (new InteractCommand(_player).Execute(_world))
            {
                RenderAll();
                return true;
            }
        }

        var command = InputHandler.GetMoveCommand(keyboard, _player);
        if (command is not null)
        {
            var acted = command.Execute(_world);
            if (acted && _mode == GameMode.Dungeon)
            {
                UpdateFieldOfView();
                _turnManager.RunEnemyTurns(_world);
            }

            RenderAll();
            return true;
        }

        return base.ProcessKeyboard(keyboard);
    }

    private bool TryHandleItemInput(Keyboard keyboard)
    {
        if (keyboard.IsKeyPressed(Keys.G))
        {
            return new PickupCommand(_player).Execute(_world);
        }

        for (var slot = 1; slot <= 9; slot++)
        {
            var key = (Keys)((int)Keys.D1 + slot - 1);
            if (!keyboard.IsKeyPressed(key))
            {
                continue;
            }

            if (keyboard.IsKeyDown(Keys.U))
            {
                return new UseItemCommand(_player, slot).Execute(_world);
            }

            if (keyboard.IsKeyDown(Keys.E))
            {
                return new EquipItemCommand(_player, slot).Execute(_world, _project.Items);
            }
        }

        return false;
    }

    private void TryEnterOverworldCell()
    {
        if (_overworldService is null ||
            !_player.TryGetComponent<PositionComponent>(out var position) ||
            position is null)
        {
            return;
        }

        var cell = _overworldService.GetCellAt(_world.Map, position.Position);
        if (cell is null)
        {
            _world.Log.Add("No region here.");
            return;
        }

        _mode = GameMode.Dungeon;
        _seed = Random.Shared.Next();
        ApplySetup(WorldBuilder.CreateDungeonFromCell(_project, cell, RuntimeBootstrap.Scripts, _seed));
        UpdateFieldOfView();
        _world.Log.Add("Press G to pick up items, U+1-9 to use, E+1-9 to equip, Space to interact.");
    }

    private void UpdateFieldOfView()
    {
        if (!_player.TryGetComponent<PositionComponent>(out var positionComponent) || positionComponent is null)
        {
            return;
        }

        var visible = FovCalculator.Compute(_world.Map, positionComponent.Position, FovRadius);
        _world.Visibility.ApplyVisible(visible);
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

        _mode = GameMode.Dungeon;
        ApplySetup(WorldBuilder.CreateFromSave(_project, saveData, RuntimeBootstrap.Scripts));
        UpdateFieldOfView();
        RenderAll();
    }

    private void ApplySetup(GameSetup setup)
    {
        _project = setup.Project;
        _world = setup.World;
        _world.Rules ??= setup.Rules;
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
