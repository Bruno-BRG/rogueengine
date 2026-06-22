using System.Diagnostics;
using RogueEngine.BuildTool;

namespace RogueEngine.Editor.Services;

public sealed class PlaytestService : IDisposable
{
    private Process? _activeProcess;

    public bool IsRunning => _activeProcess is { HasExited: false };

    public event Action? RunningStateChanged;

    public void Start(string reprojPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reprojPath);
        Stop();

        ProcessStartInfo startInfo;
        if (RuntimeProjectLocator.TryLocateRuntimeTemplate(out var templateDirectory))
        {
            var runtimeExe = Path.Combine(templateDirectory, "RogueEngine.Runtime.exe");
            startInfo = new ProcessStartInfo
            {
                FileName = runtimeExe,
                Arguments = $"\"{reprojPath}\"",
                UseShellExecute = false,
                CreateNoWindow = false
            };
        }
        else
        {
            var runtimeCsproj = RuntimeProjectLocator.LocateRuntimeCsproj();
            startInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"run --project \"{runtimeCsproj}\" -- \"{reprojPath}\"",
                UseShellExecute = false,
                CreateNoWindow = false
            };
        }

        _activeProcess = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Failed to start playtest process.");

        _activeProcess.EnableRaisingEvents = true;
        _activeProcess.Exited += OnProcessExited;
        RunningStateChanged?.Invoke();
    }

    public void Stop()
    {
        if (_activeProcess is null)
        {
            return;
        }

        try
        {
            if (!_activeProcess.HasExited)
            {
                _activeProcess.Kill(entireProcessTree: true);
            }
        }
        catch
        {
            // Process may already have exited.
        }
        finally
        {
            _activeProcess.Exited -= OnProcessExited;
            _activeProcess.Dispose();
            _activeProcess = null;
            RunningStateChanged?.Invoke();
        }
    }

    public void Dispose() => Stop();

    private void OnProcessExited(object? sender, EventArgs e) => Stop();
}
