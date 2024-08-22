namespace vein;

using static Spectre.Console.AnsiConsole;



[ExcludeFromCodeCoverage]
public static class Log
{
    public static void Info(string s) => MarkupLine($"[aqua]INFO[/]: {s}");
    public static void Warn(string s) => MarkupLine($"[orange]WARN[/]: {s}");
    public static void Error(string s) => MarkupLine($"[red]ERROR[/]: {s}");
    public static void Error(Exception s) => WriteException(s);
}
