namespace ishtar;

public readonly struct Profiler : IDisposable
{
#if PROFILER
    private readonly string _tag;
    private readonly Stopwatch _watcher;
#endif

    private Profiler(string tag)
    {
#if PROFILER
        _tag = tag;
        _watcher = Stopwatch.StartNew();
#endif
    }

    public static Profiler Begin(string tag)
        => new Profiler(tag);

    public void Complete()
    {
#if PROFILER
        _watcher.Stop();
        if (Environment.GetEnvironmentVariable("VM_PROFILER") is null)
            return;
        Console.WriteLine(_watcher.ElapsedMilliseconds > 0
            ? $"[{_tag}] elapsed {_watcher.Elapsed.TotalMilliseconds:##.###} ms"
            : $"[{_tag}] elapsed {_watcher.Elapsed.TotalMicroseconds} us");
#endif
    }

    public void Dispose() => Complete();
}
