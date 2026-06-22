using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using RogueEngine.Editor.Models;
using RogueEngine.Engine.Core;

namespace RogueEngine.Editor.Controls;

public partial class SceneMapViewport : UserControl
{
    public static readonly StyledProperty<EditorScene?> SceneProperty =
        AvaloniaProperty.Register<SceneMapViewport, EditorScene?>(nameof(Scene));

    public static readonly StyledProperty<IReadOnlyList<EditorActor>?> ActorsProperty =
        AvaloniaProperty.Register<SceneMapViewport, IReadOnlyList<EditorActor>?>(nameof(Actors));

    public static readonly StyledProperty<IReadOnlyList<EditorItem>?> ItemsProperty =
        AvaloniaProperty.Register<SceneMapViewport, IReadOnlyList<EditorItem>?>(nameof(Items));

    public static readonly StyledProperty<IReadOnlyList<EditorInteraction>?> InteractionsProperty =
        AvaloniaProperty.Register<SceneMapViewport, IReadOnlyList<EditorInteraction>?>(nameof(Interactions));

    public static readonly StyledProperty<TileMap?> MapProperty =
        AvaloniaProperty.Register<SceneMapViewport, TileMap?>(nameof(Map));

    public event EventHandler? PreviewRegenerateRequested;
    public event EventHandler<int>? SeedCommitted;
    public event EventHandler<EditorSceneEntity>? EntityPlaced;
    public event EventHandler<EditorSceneEntity>? EntityRemoved;
    public event EventHandler<EditorSceneEntity>? EntitySelected;
    public event EventHandler<MapCellEventArgs>? PlayerSpawnChanged;

    public event EventHandler<EditorSceneItem>? ItemPlaced;
    public event EventHandler<EditorSceneItem>? ItemRemoved;
    public event EventHandler<EditorSceneInteraction>? InteractionPlaced;
    public event EventHandler<EditorSceneInteraction>? InteractionRemoved;

    public SceneMapViewport()
    {
        InitializeComponent();
        MapCanvasView.EntityClicked += OnEntityClicked;
        MapCanvasView.CellClicked += OnCellClicked;
        MapCanvasView.PlayerSpawnClicked += OnPlayerSpawnClicked;
    }

    public EditorScene? Scene
    {
        get => GetValue(SceneProperty);
        set => SetValue(SceneProperty, value);
    }

    public IReadOnlyList<EditorActor>? Actors
    {
        get => GetValue(ActorsProperty);
        set => SetValue(ActorsProperty, value);
    }

    public IReadOnlyList<EditorItem>? Items
    {
        get => GetValue(ItemsProperty);
        set => SetValue(ItemsProperty, value);
    }

    public IReadOnlyList<EditorInteraction>? Interactions
    {
        get => GetValue(InteractionsProperty);
        set => SetValue(InteractionsProperty, value);
    }

    public TileMap? Map
    {
        get => GetValue(MapProperty);
        set => SetValue(MapProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == SceneProperty || change.Property == ActorsProperty ||
            change.Property == ItemsProperty || change.Property == InteractionsProperty ||
            change.Property == MapProperty)
        {
            SyncToCanvas();
        }
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        RefreshPlaceActorCombo();
        RefreshPlaceItemCombo();
        RefreshPlaceInteractionCombo();
        SyncToCanvas();
        if (Scene?.Seed is int seed)
        {
            SeedInput.Value = seed;
        }
    }

    public void RefreshPlaceActorCombo()
    {
        var previous = PlaceActorCombo.SelectedItem as string;
        var actorIds = (Actors ?? [])
            .Where(actor => !actor.IsPlayer)
            .Select(actor => actor.Id)
            .OrderBy(id => id, StringComparer.OrdinalIgnoreCase)
            .ToList();

        PlaceActorCombo.ItemsSource = new ObservableCollection<string>(actorIds);

        if (!string.IsNullOrWhiteSpace(previous) && actorIds.Contains(previous))
        {
            PlaceActorCombo.SelectedItem = previous;
        }
        else if (actorIds.Count > 0)
        {
            PlaceActorCombo.SelectedIndex = 0;
        }
    }

    public void RefreshPlaceItemCombo()
    {
        var previous = PlaceItemCombo.SelectedItem as string;
        var itemIds = (Items ?? [])
            .Select(item => item.Id)
            .OrderBy(id => id, StringComparer.OrdinalIgnoreCase)
            .ToList();

        PlaceItemCombo.ItemsSource = new ObservableCollection<string>(itemIds);

        if (!string.IsNullOrWhiteSpace(previous) && itemIds.Contains(previous))
        {
            PlaceItemCombo.SelectedItem = previous;
        }
        else if (itemIds.Count > 0)
        {
            PlaceItemCombo.SelectedIndex = 0;
        }
    }

    public void RefreshPlaceInteractionCombo()
    {
        var previous = PlaceInteractionCombo.SelectedItem as string;
        var interactionIds = (Interactions ?? [])
            .Select(interaction => interaction.Id)
            .OrderBy(id => id, StringComparer.OrdinalIgnoreCase)
            .ToList();

        PlaceInteractionCombo.ItemsSource = new ObservableCollection<string>(interactionIds);

        if (!string.IsNullOrWhiteSpace(previous) && interactionIds.Contains(previous))
        {
            PlaceInteractionCombo.SelectedItem = previous;
        }
        else if (interactionIds.Count > 0)
        {
            PlaceInteractionCombo.SelectedIndex = 0;
        }
    }

    private void SyncToCanvas()
    {
        MapCanvasView.Scene = Scene;
        MapCanvasView.Actors = Actors;
        MapCanvasView.Items = Items;
        MapCanvasView.Interactions = Interactions;
        MapCanvasView.Map = Map;
        MapCanvasView.InvalidateVisual();

        if (Scene?.Seed is int seed)
        {
            SeedInput.Value = seed;
        }

        if (Map is not null)
        {
            MapCanvasView.Width = Map.Width * MapCanvasView.CellSize;
            MapCanvasView.Height = Map.Height * MapCanvasView.CellSize;
        }
    }

    private void OnRegenerateClicked(object? sender, RoutedEventArgs e) =>
        PreviewRegenerateRequested?.Invoke(this, EventArgs.Empty);

    private void OnSeedChanged(object? sender, NumericUpDownValueChangedEventArgs e)
    {
        if (Scene is null || e.NewValue is not decimal decimalSeed)
        {
            return;
        }

        var seed = (int)decimalSeed;
        Scene.Seed = seed;
        SeedCommitted?.Invoke(this, seed);
    }

    private void OnPlaceActorChanged(object? sender, SelectionChangedEventArgs e)
    {
        MapCanvasView.PlaceActorId = PlaceActorCombo.SelectedItem as string;
    }

    private void OnPlaceItemChanged(object? sender, SelectionChangedEventArgs e)
    {
        MapCanvasView.PlaceItemId = PlaceItemCombo.SelectedItem as string;
        if (!string.IsNullOrWhiteSpace(MapCanvasView.PlaceItemId))
        {
            MapCanvasView.PlaceInteractionId = null;
            PlaceInteractionCombo.SelectedItem = null;
        }
    }

    private void OnPlaceInteractionChanged(object? sender, SelectionChangedEventArgs e)
    {
        MapCanvasView.PlaceInteractionId = PlaceInteractionCombo.SelectedItem as string;
        if (!string.IsNullOrWhiteSpace(MapCanvasView.PlaceInteractionId))
        {
            MapCanvasView.PlaceItemId = null;
            PlaceItemCombo.SelectedItem = null;
            MapCanvasView.PlaceActorId = null;
            PlaceActorCombo.SelectedItem = null;
        }
    }

    private void OnCellClicked(object? sender, MapCellEventArgs e)
    {
        if (Scene is null)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(MapCanvasView.PlaceInteractionId))
        {
            var existingInteraction = Scene.InteractionPlacements.FirstOrDefault(
                interaction => interaction.X == e.X && interaction.Y == e.Y);
            if (existingInteraction is not null)
            {
                Scene.InteractionPlacements.Remove(existingInteraction);
                InteractionRemoved?.Invoke(this, existingInteraction);
                MapCanvasView.InvalidateVisual();
                return;
            }

            var interactionPlacement = new EditorSceneInteraction
            {
                InteractionId = MapCanvasView.PlaceInteractionId,
                X = e.X,
                Y = e.Y
            };
            Scene.InteractionPlacements.Add(interactionPlacement);
            InteractionPlaced?.Invoke(this, interactionPlacement);
            MapCanvasView.InvalidateVisual();
            return;
        }

        if (!string.IsNullOrWhiteSpace(MapCanvasView.PlaceItemId))
        {
            var existingItem = Scene.ItemPlacements.FirstOrDefault(item => item.X == e.X && item.Y == e.Y);
            if (existingItem is not null)
            {
                Scene.ItemPlacements.Remove(existingItem);
                ItemRemoved?.Invoke(this, existingItem);
                MapCanvasView.InvalidateVisual();
                return;
            }

            var itemPlacement = new EditorSceneItem
            {
                ItemId = MapCanvasView.PlaceItemId,
                X = e.X,
                Y = e.Y,
                Count = 1
            };
            Scene.ItemPlacements.Add(itemPlacement);
            ItemPlaced?.Invoke(this, itemPlacement);
            MapCanvasView.InvalidateVisual();
            return;
        }

        if (string.IsNullOrWhiteSpace(MapCanvasView.PlaceActorId))
        {
            return;
        }

        var existing = Scene.Entities.FirstOrDefault(entity => entity.X == e.X && entity.Y == e.Y);
        if (existing is not null)
        {
            Scene.Entities.Remove(existing);
            EntityRemoved?.Invoke(this, existing);
            MapCanvasView.InvalidateVisual();
            return;
        }

        var placement = new EditorSceneEntity
        {
            ActorId = MapCanvasView.PlaceActorId,
            X = e.X,
            Y = e.Y
        };
        Scene.Entities.Add(placement);
        EntityPlaced?.Invoke(this, placement);
        MapCanvasView.InvalidateVisual();
    }

    private void OnPlayerSpawnClicked(object? sender, MapCellEventArgs e)
    {
        if (Scene is null)
        {
            return;
        }

        Scene.PlayerSpawnX = e.X;
        Scene.PlayerSpawnY = e.Y;
        PlayerSpawnChanged?.Invoke(this, e);
        MapCanvasView.InvalidateVisual();
    }

    private void OnEntityClicked(object? sender, EditorSceneEntity e) =>
        EntitySelected?.Invoke(this, e);
}

public sealed class MapCellEventArgs : EventArgs
{
    public required int X { get; init; }
    public required int Y { get; init; }
}

public sealed class MapCanvasControl : Control
{
    private static readonly IBrush FloorBrush = new SolidColorBrush(Color.Parse("#2a2a32"));
    private static readonly IBrush WallBrush = new SolidColorBrush(Color.Parse("#5a5a68"));
    private static readonly IBrush GridBrush = new SolidColorBrush(Color.Parse("#1a1a20"));
    private static readonly IBrush PlayerBrush = new SolidColorBrush(Color.Parse("#5b8def"));

    private bool _isPanning;
    private Point _lastPanPoint;
    private bool _spaceHeld;

    public double CellSize { get; set; } = 16;
    public EditorScene? Scene { get; set; }
    public IReadOnlyList<EditorActor>? Actors { get; set; }
    public IReadOnlyList<EditorItem>? Items { get; set; }
    public IReadOnlyList<EditorInteraction>? Interactions { get; set; }
    public TileMap? Map { get; set; }
    public string? PlaceActorId { get; set; }
    public string? PlaceItemId { get; set; }
    public string? PlaceInteractionId { get; set; }

    public event EventHandler<MapCellEventArgs>? CellClicked;
    public event EventHandler<MapCellEventArgs>? PlayerSpawnClicked;
    public event EventHandler<EditorSceneEntity>? EntityClicked;

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        Focus();
        var point = e.GetCurrentPoint(this);
        if (point.Properties.IsMiddleButtonPressed || (_spaceHeld && point.Properties.IsLeftButtonPressed))
        {
            _isPanning = true;
            _lastPanPoint = point.Position;
            e.Handled = true;
            return;
        }

        if (!point.Properties.IsLeftButtonPressed || Map is null)
        {
            return;
        }

        if (!TryGetCell(point.Position, out var x, out var y))
        {
            return;
        }

        if (e.KeyModifiers.HasFlag(KeyModifiers.Shift))
        {
            PlayerSpawnClicked?.Invoke(this, new MapCellEventArgs { X = x, Y = y });
            e.Handled = true;
            return;
        }

        var entity = Scene?.Entities.FirstOrDefault(item => item.X == x && item.Y == y);
        if (entity is not null && string.IsNullOrWhiteSpace(PlaceActorId))
        {
            EntityClicked?.Invoke(this, entity);
            e.Handled = true;
            return;
        }

        if (!string.IsNullOrWhiteSpace(PlaceInteractionId) && Map.IsWalkable(x, y))
        {
            CellClicked?.Invoke(this, new MapCellEventArgs { X = x, Y = y });
            e.Handled = true;
            return;
        }

        if (!string.IsNullOrWhiteSpace(PlaceItemId) && Map.IsWalkable(x, y))
        {
            CellClicked?.Invoke(this, new MapCellEventArgs { X = x, Y = y });
            e.Handled = true;
            return;
        }

        if (!string.IsNullOrWhiteSpace(PlaceActorId) && Map.IsWalkable(x, y))
        {
            CellClicked?.Invoke(this, new MapCellEventArgs { X = x, Y = y });
            e.Handled = true;
        }
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        _isPanning = false;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (!_isPanning)
        {
            return;
        }

        var point = e.GetCurrentPoint(this);
        var delta = point.Position - _lastPanPoint;
        _lastPanPoint = point.Position;

        if (Parent is ScrollViewer scroll)
        {
            scroll.Offset = new Vector(scroll.Offset.X - delta.X, scroll.Offset.Y - delta.Y);
        }
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);
        var next = CellSize + (e.Delta.Y > 0 ? 2 : -2);
        CellSize = Math.Clamp(next, 8, 32);
        if (Map is not null)
        {
            Width = Map.Width * CellSize;
            Height = Map.Height * CellSize;
        }

        InvalidateVisual();
        e.Handled = true;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Key == Key.Space)
        {
            _spaceHeld = true;
        }
    }

    protected override void OnKeyUp(KeyEventArgs e)
    {
        base.OnKeyUp(e);
        if (e.Key == Key.Space)
        {
            _spaceHeld = false;
            _isPanning = false;
        }
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        if (Map is null)
        {
            return;
        }

        for (var x = 0; x < Map.Width; x++)
        {
            for (var y = 0; y < Map.Height; y++)
            {
                var tile = Map.GetTile(x, y);
                var brush = tile.IsWalkable ? FloorBrush : WallBrush;
                var rect = new Rect(x * CellSize, y * CellSize, CellSize, CellSize);
                context.FillRectangle(brush, rect);
                context.DrawRectangle(GridBrush, null, rect, 0.5, 0.5);
            }
        }

        if (Scene?.PlayerSpawnX is int spawnX && Scene.PlayerSpawnY is int spawnY)
        {
            var rect = new Rect(spawnX * CellSize + 2, spawnY * CellSize + 2, CellSize - 4, CellSize - 4);
            context.FillRectangle(PlayerBrush, rect);
        }

        if (Scene is not null && Actors is not null)
        {
            foreach (var entity in Scene.Entities)
            {
                var actor = Actors.FirstOrDefault(item => item.Id == entity.ActorId);
                if (actor is null)
                {
                    continue;
                }

                var glyph = new FormattedText(
                    actor.Glyph.ToString(),
                    System.Globalization.CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight,
                    new Typeface(FontFamily.Default),
                    CellSize * 0.75,
                    new SolidColorBrush(Color.FromRgb(actor.ColorR, actor.ColorG, actor.ColorB)));

                context.DrawText(glyph, new Point(entity.X * CellSize + 2, entity.Y * CellSize + 1));
            }

            foreach (var placement in Scene.ItemPlacements)
            {
                var item = Items?.FirstOrDefault(entry => entry.Id == placement.ItemId);
                if (item is null)
                {
                    continue;
                }

                var glyph = new FormattedText(
                    item.Glyph.ToString(),
                    System.Globalization.CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight,
                    new Typeface(FontFamily.Default),
                    CellSize * 0.75,
                    new SolidColorBrush(Color.FromRgb(item.ColorR, item.ColorG, item.ColorB)));

                context.DrawText(glyph, new Point(placement.X * CellSize + 2, placement.Y * CellSize + 1));
            }

            foreach (var placement in Scene.InteractionPlacements)
            {
                var glyph = new FormattedText(
                    "+",
                    System.Globalization.CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight,
                    new Typeface(FontFamily.Default),
                    CellSize * 0.75,
                    new SolidColorBrush(Color.Parse("#c8b478")));

                context.DrawText(glyph, new Point(placement.X * CellSize + 2, placement.Y * CellSize + 1));
            }
        }
    }

    private bool TryGetCell(Point position, out int x, out int y)
    {
        x = (int)(position.X / CellSize);
        y = (int)(position.Y / CellSize);
        return Map is not null && x >= 0 && y >= 0 && x < Map.Width && y < Map.Height;
    }
}
