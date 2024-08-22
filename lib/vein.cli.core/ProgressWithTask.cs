namespace vein;

using Spectre.Console;

public class ProgressWithTask(ProgressTask task, string fmt) : IProgress<(int total, int speed)>
{
    public void Report((int total, int speed) value)
    {
        if (!task.IsStarted)
            task.StartTask();
        if (value.total <= 99)
            task.Description(fmt.Replace("{%bytes}", value.speed.FormatBytesPerSecond())).Value(value.total);
        else
            task.Description(fmt.Replace("{%bytes}", "")).Value(value.total);
    }

    public static ProgressWithTask Create(ProgressContext ctx, string template)
        => new(ctx.AddTask(template, false), template);

    public static async Task<T> Progress<T>(Func<IProgress<(int total, int speed)>, ValueTask<T>> actor, string template)
        => await AnsiConsole.Progress().AutoClear(false).Columns(
            new ProgressBarColumn(),
            new PercentageColumn(),
            new SpinnerColumn { Spinner = Spinner.Known.Dots8Bit, CompletedText = "✅", FailedText = "❌" },
            new TaskDescriptionColumn { Alignment = Justify.Left }).AutoRefresh(true).StartAsync(async (x) => await actor(Create(x, template)));
}
