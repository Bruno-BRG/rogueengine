namespace RogueEngine.Editor.Models;

public sealed class RecentProjectEntry
{
    public string Name { get; set; } = string.Empty;
    public string ReprojPath { get; set; } = string.Empty;
    public DateTime LastOpenedUtc { get; set; }
    public string? ThumbnailPath { get; set; }

    public string LastOpenedDisplay =>
        LastOpenedUtc == default
            ? "Never"
            : LastOpenedUtc.ToLocalTime().ToString("g");
}
