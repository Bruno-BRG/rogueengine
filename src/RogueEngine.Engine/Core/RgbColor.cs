namespace RogueEngine.Engine.Core;

public readonly record struct RgbColor(byte R, byte G, byte B)
{
    public static RgbColor White => new(255, 255, 255);
    public static RgbColor Yellow => new(255, 255, 0);
    public static RgbColor Gray => new(128, 128, 128);
}
