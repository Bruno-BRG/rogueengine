namespace RogueEngine.BuildTool;

public static class BuildLogger
{
    public static void Info(string message) => Write(BuildLogLevel.Info, message);
    public static void Success(string message) => Write(BuildLogLevel.Success, message);
    public static void Warning(string message) => Write(BuildLogLevel.Warning, message);
    public static void Error(string message) => Write(BuildLogLevel.Error, message);

    private static void Write(BuildLogLevel level, string message)
    {
        var sink = BuildLogScope.CurrentSink;
        if (sink is not null)
        {
            sink.Write(level, message);
            return;
        }

        WriteToConsole(level, message);
    }

    private static void WriteToConsole(BuildLogLevel level, string message)
    {
        var prefix = level switch
        {
            BuildLogLevel.Success => "OK",
            BuildLogLevel.Error => "ERROR",
            BuildLogLevel.Warning => "WARN",
            _ => "BUILD"
        };

        var previousColor = Console.ForegroundColor;
        Console.ForegroundColor = level switch
        {
            BuildLogLevel.Success => ConsoleColor.Green,
            BuildLogLevel.Error => ConsoleColor.Red,
            BuildLogLevel.Warning => ConsoleColor.Yellow,
            _ => ConsoleColor.Cyan
        };

        Console.WriteLine($"[{prefix}] {message}");
        Console.ForegroundColor = previousColor;
    }
}
