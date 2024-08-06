namespace vein;

using project;
using Spectre.Console;
using Spectre.Console.Cli;
using styles;

public abstract class AsyncCommandWithProject<T> : CommandWithProject<T> where T : CommandSettings, IProjectSettingProvider
{
    public sealed override int Execute(CommandContext ctx, T settings, VeinProject project)
        => ExecuteAsync(ctx, settings, project).Result;

    public abstract Task<int> ExecuteAsync(CommandContext ctx, T settings, VeinProject project);
}


public interface IProgressionTask
{
    IProgressionTask IsIndeterminate(bool has);

    double MaxValue { get; set; }

    IProgressionTask Increment(double value);

    void VeinStatus(string status);

    void Description(string description);

    void StopTask();

    void SuccessTask();

    void FailTask();

    void Value(double value);

    IProgressionTask AddChildTask(string description, bool autoStart = true, double maxValue = 100,
        bool allowHide = true);

    IProgressionTask WithState<T>(string key, T val) where T : class;
}
