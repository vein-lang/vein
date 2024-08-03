namespace vein;

using project;
using Spectre.Console.Cli;

public abstract class AsyncCommandWithProject<T> : CommandWithProject<T> where T : CommandSettings, IProjectSettingProvider
{
    public sealed override int Execute(CommandContext ctx, T settings, VeinProject project)
        => ExecuteAsync(ctx, settings, project).Result;

    public abstract Task<int> ExecuteAsync(CommandContext ctx, T settings, VeinProject project);
}
