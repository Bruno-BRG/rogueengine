using System.Diagnostics;
using RogueEngine.BuildTool;

namespace RogueEngine.Editor.Services;

public sealed class PlaytestService
{
    public Process Start(string reprojPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reprojPath);

        var runtimeCsproj = RuntimeProjectLocator.LocateRuntimeCsproj();
        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"run --project \"{runtimeCsproj}\" -- \"{reprojPath}\"",
            UseShellExecute = false,
            CreateNoWindow = false
        };

        return Process.Start(startInfo)
            ?? throw new InvalidOperationException("Failed to start playtest process.");
    }
}
