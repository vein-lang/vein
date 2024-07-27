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

    public static ProgressTask WithState<T>(this ProgressTask task, string key, T state) where T : class
    {
        task.State.Update<SharedContainer<T>>(key, (_) => new(state));
        return task;
    }

    public static T Get<T>(this ProgressTaskState state, string key) where T : class
        => state.Get<SharedContainer<T>>(key).Value;

    public readonly struct SharedContainer<T>
    {
        public T Value { get; }

        public SharedContainer(T value) => Value = value;
    }
}
