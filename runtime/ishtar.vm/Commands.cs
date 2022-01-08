namespace ishtar.vm;

[ExcludeFromCodeCoverage]
public static class Commands
{
    public static void DisplayDefinedMapping()
    {
        if (!HasFlag(SysFlag.DISPLAY_FFI_MAPPING)) return;

        foreach (var (key, value) in FFI.method_table)
            print($"ffi map '{key}' -> 'sys::FFI/{value.Name}'");
    }


    public static bool HasFlag(SysFlag flag)
    {
        var key = _cache.ContainsKey(flag) ?
            _cache[flag] :
            _cache[flag] = $"--sys::{flag.ToString().ToLowerInvariant().Replace("_", "-")}";

        return Environment.GetEnvironmentVariable(key) is not null;
    }


    private static void print(string s) => Console.WriteLine(s);

    private static Dictionary<SysFlag, string> _cache = new Dictionary<SysFlag, string>(64);
}


public enum SysFlag
{
    DISPLAY_FFI_MAPPING
}
