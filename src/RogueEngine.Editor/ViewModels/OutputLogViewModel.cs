using RogueEngine.BuildTool;

namespace RogueEngine.Editor.ViewModels;

public sealed class OutputLogViewModel : ViewModelBase
{
    private readonly List<LogEntry> _entries = [];

    public IReadOnlyList<LogEntry> Entries => _entries;

    public event Action? Changed;

    public void Clear()
    {
        _entries.Clear();
        Changed?.Invoke();
    }

    public void Add(BuildLogLevel level, string message)
    {
        _entries.Add(new LogEntry(level, message));
        Changed?.Invoke();
    }

    public void AddInfo(string message) => Add(BuildLogLevel.Info, message);
    public void AddSuccess(string message) => Add(BuildLogLevel.Success, message);
    public void AddWarning(string message) => Add(BuildLogLevel.Warning, message);
    public void AddError(string message) => Add(BuildLogLevel.Error, message);

    public string Text => string.Join(Environment.NewLine, _entries.Select(entry => $"[{entry.Level}] {entry.Message}"));
}

public readonly record struct LogEntry(BuildLogLevel Level, string Message);

public sealed class EditorLogSink(OutputLogViewModel log) : IBuildLogSink
{
    public void Write(BuildLogLevel level, string message) => log.Add(level, message);
}
