namespace ishtar;

using runtime;
using vein.reflection;
using vein.runtime;

public static unsafe partial class KnowTypes
{
    public static class VeinLang
    {
        public static class Reflection
        {}

        public static class Native
        {
            public static QualityTypeName NativeHandleTypeName =
                new QualityTypeName("std", nameof(NativeHandle), "global::vein/lang/native");

            public static RuntimeIshtarClass* NativeHandle(CallFrame frame)
                => findType(NativeHandleTypeName, frame);
        }


        public static QualityTypeName FileNotFoundFaultTypeName =
            new QualityTypeName("std", nameof(FileNotFoundFault), "global::vein/lang");

        public static RuntimeIshtarClass* FileNotFoundFault(CallFrame frame)
            => findType(FileNotFoundFaultTypeName, frame);
    }


    public static QualityTypeName TypeInfoTypeName =
        new QualityTypeName("std", nameof(Type), "global::vein/lang");
    public static QualityTypeName FieldInfoTypeName =
        new QualityTypeName("std", nameof(Field), "global::vein/lang");
    public static QualityTypeName FunctionInfoTypeName =
        new QualityTypeName("std", nameof(Function), "global::vein/lang");

    public static QualityTypeName NullPointerExceptionTypeName =
        new QualityTypeName("std", "NullPointerException", "global::vein/lang");
    public static QualityTypeName IncorrectCastFaultTypeName =
        new QualityTypeName("std", "IncorrectCastFault", "global::vein/lang");
    public static QualityTypeName FreeImmortalObjectFaultTypeName =
        new QualityTypeName("std", nameof(FreeImmortalObjectFault), "global::vein/lang");
    public static QualityTypeName TypeNotFoundFaultTypeName =
        new QualityTypeName("std", nameof(TypeNotFoundFault), "global::vein/lang/reflection");
    public static QualityTypeName MultipleTypeFoundFaultTypeName =
        new QualityTypeName("std", nameof(MultipleTypeFoundFault), "global::vein/lang/reflection");
    public static QualityTypeName PlatformIsNotSupportFaultTypeName =
        new QualityTypeName("std", nameof(PlatformIsNotSupportFault), "global::vein/lang");
    public static QualityTypeName IshatFault =
        new QualityTypeName("std", nameof(IshatFault), "global::vein/lang");

    public static RuntimeIshtarClass* NullPointerException(CallFrame frame)
        => findType(NullPointerExceptionTypeName, frame);

    public static RuntimeIshtarClass* IncorrectCastFault(CallFrame frame)
        => findType(IncorrectCastFaultTypeName, frame);

    public static RuntimeIshtarClass* FreeImmortalObjectFault(CallFrame frame)
        => findType(FreeImmortalObjectFaultTypeName, frame);

    public static RuntimeIshtarClass* TypeNotFoundFault(CallFrame frame)
        => findType(TypeNotFoundFaultTypeName, frame);

    public static RuntimeIshtarClass* MultipleTypeFoundFault(CallFrame frame)
        => findType(MultipleTypeFoundFaultTypeName, frame);

    public static RuntimeIshtarClass* PlatformIsNotSupportFault(CallFrame frame)
        => findType(PlatformIsNotSupportFaultTypeName, frame);

    public static RuntimeIshtarClass* NativeFault(CallFrame frame)
        => findType(IshatFault, frame);

    public static RuntimeIshtarClass* Type(CallFrame frame)
        => findType(TypeInfoTypeName, frame);
    public static RuntimeIshtarClass* Field(CallFrame frame)
        => findType(FieldInfoTypeName, frame);
    public static RuntimeIshtarClass* Function(CallFrame frame)
        => findType(FunctionInfoTypeName, frame);


    private static readonly Dictionary<nint, nint> _cache = new();



    public static RuntimeIshtarClass* FromCache(QualityTypeName q, CallFrame frame)
        => findType(q.T(), frame);

    public static RuntimeIshtarClass* FromCache(RuntimeQualityTypeName* q, CallFrame frame)
        => findType(q, frame);

    private static RuntimeIshtarClass* findType(QualityTypeName q, CallFrame frame)
        => findType(q.T(), frame);

    private static RuntimeIshtarClass* findType(RuntimeQualityTypeName* q, CallFrame frame)
    {
        if (_cache.TryGetValue((nint)q, out IntPtr type))
            return (RuntimeIshtarClass*)type;

        var t = frame.method->Owner->Owner->FindType(q, true, false);

        if (t->IsUnresolved)
        {
            frame.vm.FastFail(WNE.MISSING_TYPE, $"Cannot find '{q->NameWithNS}' bulitin type", frame);
            return null;
        }
        
        return (RuntimeIshtarClass*)(_cache[(nint)q] = (nint)t);
    }
}
