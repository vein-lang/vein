namespace vein.cmd;

using Spectre.Console;
using Spectre.Console.Cli;

public class TelemetryCommand : Command
{
    public override int Execute(CommandContext context)
    {
        AnsiConsole.Write(new Rule($"[red bold]Telemetry Notice[/]") { Style = Style.Parse("orange rapidblink") });
        AnsiConsole.MarkupLine($"");
        AnsiConsole.MarkupLine($"\tThe [blue bold]VeinSDK[/] tools collect usage data in order to help us improve your experience.");
        AnsiConsole.MarkupLine($"\tYou [red bold]cannot disable[/] telemetry before the [blue bold]1.0[/] release.");
        AnsiConsole.MarkupLine($"\tIf you [red bold]do not agree[/] with this notification, [red bold]stop[/] the installation and [red bold]delete[/] the [gray bold]~/.vein[/] folder");
        AnsiConsole.MarkupLine($"");
        AnsiConsole.Write(new Rule { Style = Style.Parse("orange rapidblink") });
        return 0;
    }
}
