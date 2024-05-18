namespace ishtar;

[ExcludeFromCodeCoverage]
public static class Commands
{
    private static void print(string s) => Console.WriteLine(s);

    private static Dictionary<SysFlag, string> _cache = new Dictionary<SysFlag, string>(64);
}


public enum SysFlag
{
    DISPLAY_FFI_MAPPING,
    ENABLED_STATIC_CTOR
}
