namespace RogueEngine.BuildTool;

public enum BuildLogLevel
{
    Info,
    Success,
    Warning,
    Error
}

public interface IBuildLogSink
{
    void Write(BuildLogLevel level, string message);
}
