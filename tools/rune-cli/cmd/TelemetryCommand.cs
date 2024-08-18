namespace vein.cmd;

using Spectre.Console;
using Spectre.Console.Cli;

public class TelemetryCommand : Command
{
    public override int Execute(CommandContext context)
    {
        AnsiConsole.Write(new Rule($"[blue bold]Vein Telemetry[/]") { Style = Style.Parse("lime rapidblink") });
        AnsiConsole.MarkupLine($"The [blue bold]VeinSDK[/] tools collect usage data in order to help us improve your experience.");
        AnsiConsole.MarkupLine($"You [red bold]cannot disable[/] telemetry before the [blue bold]1.0[/] release.");
        AnsiConsole.Write(new Rule { Style = Style.Parse("lime rapidblink") });
        return 0;
    }
}
