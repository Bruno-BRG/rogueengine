using RogueEngine.Engine.Core;

namespace RogueEngine.Engine.Data;

public sealed class ColorData
{
    public byte R { get; init; }
    public byte G { get; init; }
    public byte B { get; init; }

    public RgbColor ToRgbColor() => new(R, G, B);
}
