namespace vein.styles;

using Spectre.Console;

public static class ProgressTaskExtension
{
    public static ProgressTask FailTask(this ProgressTask task)
    {
        task.StopTask(true);
        return task;
    }
    public static ProgressTask SuccessTask(this ProgressTask task)
    {
        task.StopTask(false);
        return task;
    }
}
