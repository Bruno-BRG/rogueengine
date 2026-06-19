namespace RogueEngine.BuildTool;

public static class BuildLogger
{
    public static void Info(string message) => Write("BUILD", message);
    public static void Success(string message) => Write("OK", message);
    public static void Warning(string message) => Write("WARN", message);
    public static void Error(string message) => Write("ERROR", message);

    private static void Write(string prefix, string message)
    {
        var previousColor = Console.ForegroundColor;
        Console.ForegroundColor = prefix switch
        {
            "OK" => ConsoleColor.Green,
            "ERROR" => ConsoleColor.Red,
            "WARN" => ConsoleColor.Yellow,
            _ => ConsoleColor.Cyan
        };

        Console.WriteLine($"[{prefix}] {message}");
        Console.ForegroundColor = previousColor;
    }
}
