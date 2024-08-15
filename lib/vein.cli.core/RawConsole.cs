namespace vein;

using Spectre.Console;

public class RawConsole
{
    public static IAnsiConsole Create() =>
        AnsiConsole.Create(new AnsiConsoleSettings
        {
            Ansi = AnsiSupport.No,
            ColorSystem = (ColorSystemSupport)ColorSystem.NoColors,
            Out = new AnsiConsoleOutput(Console.Out),
            Interactive = InteractionSupport.No,
            ExclusivityMode = null,
            Enrichment = new ProfileEnrichment
            {
                UseDefaultEnrichers = false,
            },
        });

    public static IAnsiConsole CreateForkConsole() =>
        AnsiConsole.Create(new AnsiConsoleSettings
        {
            Ansi = AnsiSupport.Yes,
            ColorSystem = (ColorSystemSupport)ColorSystem.TrueColor,
            Out = new ForkConsole(Console.Out),
            Interactive = InteractionSupport.Yes
        });
}

public class ForkConsole(TextWriter writer) : AnsiConsoleOutput(writer)
{
    public override bool IsTerminal => true;
    public override int Width => int.Parse(Environment.GetEnvironmentVariable("FORK_CONSOLE_W")!);
    public override int Height => int.Parse(Environment.GetEnvironmentVariable("FORK_CONSOLE_H")!);
}
