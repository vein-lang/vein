namespace ishtar.jit;

internal static class _ext
{
    public static T[] init_with<T>(this T[] a, Func<int, T> fn)
    {
        for (var i = 0; i < a.Length; i++) a[i] = fn(i);
        return a;
    }
    public static T[] init<T>(this T[] a) where T : new()
    {
        for (var i = 0; i < a.Length; i++) a[i] = new T();
        return a;
    }

    public static bool IsSet(this int value, int flag)
        => (value & flag) != 0;

    public static bool IsSet(this long value, long flag)
        => (value & flag) != 0;
}
