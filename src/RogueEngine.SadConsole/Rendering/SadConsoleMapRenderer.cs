using RogueEngine.Engine.Components;
using RogueEngine.Engine.Core;
using RogueEngine.Engine.Rendering;
using SadConsole;
using SadRogue.Primitives;

namespace RogueEngine.SadConsole.Rendering;

public sealed class SadConsoleMapRenderer : IRenderer
{
    private readonly ScreenSurface _surface;
    private readonly ColoredGlyph _tileTemplate = new(Color.DarkGray, Color.Black, '.');
    private readonly ColoredGlyph _entityTemplate = new(Color.White, Color.Black, '@');

    public SadConsoleMapRenderer(ScreenSurface surface)
    {
        _surface = surface ?? throw new ArgumentNullException(nameof(surface));
    }

    public void Render(World world)
    {
        ArgumentNullException.ThrowIfNull(world);

        _surface.Clear();

        var map = world.Map;
        var visibility = world.Visibility;
        for (var x = 0; x < map.Width; x++)
        {
            for (var y = 0; y < map.Height; y++)
            {
                var explored = visibility.IsExplored(x, y);
                var visible = visibility.IsVisible(x, y);
                var tile = map.GetTile(x, y);
                _tileTemplate.Glyph = tile.Glyph;

                if (!explored)
                {
                    _tileTemplate.Foreground = Color.Black;
                    _tileTemplate.Background = Color.Black;
                }
                else if (!visible)
                {
                    _tileTemplate.Foreground = Color.DarkGray;
                    _tileTemplate.Background = Color.Black;
                }
                else
                {
                    _tileTemplate.Foreground = Color.DarkGray;
                    _tileTemplate.Background = Color.Black;
                }

                _tileTemplate.CopyAppearanceTo(_surface.Surface[x, y]);
            }
        }

        foreach (var entity in world.Entities)
        {
            if (!entity.TryGetComponent<PositionComponent>(out var positionComponent) || positionComponent is null)
            {
                continue;
            }

            if (!entity.TryGetComponent<RenderableComponent>(out var renderable) || renderable is null)
            {
                continue;
            }

            var position = positionComponent.Position;
            if (!visibility.IsVisible(position.X, position.Y))
            {
                continue;
            }

            _entityTemplate.Glyph = renderable.Glyph;
            _entityTemplate.Foreground = ToSadColor(renderable.Foreground);
            _entityTemplate.CopyAppearanceTo(_surface.Surface[position.X, position.Y]);
        }

        _surface.IsDirty = true;
    }

    private static Color ToSadColor(RgbColor color) =>
        new(color.R, color.G, color.B);
}
