namespace ishtar;

using vein.reflection;
using vein.runtime;

public static class KnowTypes
{
    public static QualityTypeName NullPointerExceptionTypeName =
        new QualityTypeName("std", "NullPointerException", "global::vein/lang");



    public static RuntimeIshtarClass NullPointerException(CallFrame frame)
        => findType(NullPointerExceptionTypeName, frame);




    private static Dictionary<QualityTypeName, RuntimeIshtarClass> _cache =
        new Dictionary<QualityTypeName, RuntimeIshtarClass>();



    public static RuntimeIshtarClass FromCache(QualityTypeName q, CallFrame frame)
        => findType(q, frame);

    private static RuntimeIshtarClass findType(QualityTypeName q, CallFrame frame)
    {
        if (_cache.ContainsKey(q))
            return _cache[q];

        var t = frame.method.Owner.Owner.FindType(q, true, false);

        if (t is UnresolvedVeinClass)
        {
            VM.FastFail(WNE.MISSING_TYPE, $"Cannot find '{q}' bulitin type", frame);
            VM.ValidateLastError();
            return null;
        }

        if (t is not RuntimeIshtarClass r)
        {
            VM.FastFail(WNE.STATE_CORRUPT, $"'{q}' bulitin type found, but is not runtime class.", frame);
            VM.ValidateLastError();
            return null;
        }

        return _cache[q] = r;
    }
}