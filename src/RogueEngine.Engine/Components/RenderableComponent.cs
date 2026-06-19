using RogueEngine.Engine.Core;

namespace RogueEngine.Engine.Components;

public sealed class RenderableComponent : IComponent
{
    public char Glyph { get; set; }
    public RgbColor Foreground { get; set; }

    public RenderableComponent(char glyph, RgbColor foreground)
    {
        Glyph = glyph;
        Foreground = foreground;
    }
}
