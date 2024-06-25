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
}
