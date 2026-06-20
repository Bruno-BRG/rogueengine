namespace RogueEngine.BuildTool;

public sealed class BuildLogScope : IDisposable
{
    private static readonly AsyncLocal<BuildLogScope?> CurrentScope = new();

    private readonly BuildLogScope? _previous;

    public BuildLogScope(IBuildLogSink? sink)
    {
        Sink = sink;
        _previous = CurrentScope.Value;
        CurrentScope.Value = this;
    }

    public IBuildLogSink? Sink { get; }

    internal static IBuildLogSink? CurrentSink => CurrentScope.Value?.Sink;

    public void Dispose() => CurrentScope.Value = _previous;
}
