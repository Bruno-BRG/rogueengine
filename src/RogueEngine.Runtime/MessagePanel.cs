using RogueEngine.Engine.Core;
using SadConsole;
using SadRogue.Primitives;

namespace RogueEngine.Runtime;

internal sealed class MessagePanel
{
    private const int VisibleLineCount = 3;

    private readonly ScreenSurface _surface;

    public MessagePanel(int width, int height, int mapHeight)
    {
        _surface = new ScreenSurface(width, height)
        {
            UseMouse = false,
            Position = new Point(0, mapHeight)
        };
    }

    public ScreenSurface Surface => _surface;

    public void Render(MessageLog log)
    {
        ArgumentNullException.ThrowIfNull(log);

        _surface.Clear();

        var messages = log.Messages;
        var startIndex = Math.Max(0, messages.Count - VisibleLineCount);
        var line = 0;

        for (var i = startIndex; i < messages.Count; i++)
        {
            _surface.Print(0, line, messages[i]);
            line++;
        }

        _surface.IsDirty = true;
    }
}
