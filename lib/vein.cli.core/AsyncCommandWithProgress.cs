namespace vein;

using Spectre.Console;
using Spectre.Console.Cli;

public abstract class AsyncCommandWithProgress<TSettings> : AsyncCommand<TSettings>
    where TSettings : CommandSettings
{
    public sealed override Task<int> ExecuteAsync(CommandContext context, TSettings settings)
        => AnsiConsole
            .Progress()
            .StartAsync(progressContext => ExecuteAsync(progressContext, settings));

    public abstract Task<int> ExecuteAsync(ProgressContext context, TSettings settings);
}
