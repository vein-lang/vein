namespace ishtar;

using runtime;
using vein.reflection;
using vein.runtime;

public static unsafe partial class KnowTypes
{
    public static QualityTypeName TypeInfoTypeName = create("std", nameof(Type), "std");
    public static QualityTypeName FieldInfoTypeName = create("std", nameof(Field), "std");
    public static QualityTypeName FunctionInfoTypeName = create("std", nameof(Function), "std");

    public static QualityTypeName NullPointerExceptionTypeName = create("std", "NullPointerException", "std");
    public static QualityTypeName IncorrectCastFaultTypeName = create("std", "IncorrectCastFault", "std");
    public static QualityTypeName SocketFaultTypeName = create("std", "SocketFault", "std");
    public static QualityTypeName FreeImmortalObjectFaultTypeName =
        create("std", nameof(FreeImmortalObjectFault), "std");
    public static QualityTypeName TypeNotFoundFaultTypeName =
        create("std", nameof(TypeNotFoundFault), "std::reflection");
    public static QualityTypeName MultipleTypeFoundFaultTypeName =
        create("std", nameof(MultipleTypeFoundFault), "std::reflection");
    public static QualityTypeName PlatformIsNotSupportFaultTypeName =
        create("std", nameof(PlatformIsNotSupportFault), "std");
    public static QualityTypeName IshtarFault = create("std", nameof(IshtarFault), "std");


    private static QualityTypeName create(string @module, string name, string @namespace)
        => new QualityTypeName(new NameSymbol(name), new NamespaceSymbol(@namespace), new ModuleNameSymbol(module));

    public static RuntimeIshtarClass* NullPointerException(CallFrame* frame)
        => findType(NullPointerExceptionTypeName, frame);

    public static RuntimeIshtarClass* IncorrectCastFault(CallFrame* frame)
        => findType(IncorrectCastFaultTypeName, frame);

    public static RuntimeIshtarClass* FreeImmortalObjectFault(CallFrame* frame)
        => findType(FreeImmortalObjectFaultTypeName, frame);

    public static RuntimeIshtarClass* TypeNotFoundFault(CallFrame* frame)
        => findType(TypeNotFoundFaultTypeName, frame);

    public static RuntimeIshtarClass* MultipleTypeFoundFault(CallFrame* frame)
        => findType(MultipleTypeFoundFaultTypeName, frame);

    public static RuntimeIshtarClass* PlatformIsNotSupportFault(CallFrame* frame)
        => findType(PlatformIsNotSupportFaultTypeName, frame);

    public static RuntimeIshtarClass* NativeFault(CallFrame* frame)
        => findType(IshtarFault, frame);

    public static RuntimeIshtarClass* SocketFault(CallFrame* frame)
        => findType(SocketFaultTypeName, frame);

    public static RuntimeIshtarClass* Type(CallFrame* frame)
        => findType(TypeInfoTypeName, frame);
    public static RuntimeIshtarClass* Field(CallFrame* frame)
        => findType(FieldInfoTypeName, frame);
    public static RuntimeIshtarClass* Function(CallFrame* frame)
        => findType(FunctionInfoTypeName, frame);


    private static readonly Dictionary<nint, nint> _cache = new();


    
    public static RuntimeIshtarClass* FromCache(RuntimeQualityTypeName* q, CallFrame* frame)
        => findType(q, frame);

    private static RuntimeIshtarClass* findType(QualityTypeName q, CallFrame* frame)
        => findType(q.T(frame), frame);

    private static RuntimeIshtarClass* findType(RuntimeQualityTypeName* q, CallFrame* frame)
    {
        if (_cache.TryGetValue((nint)q, out IntPtr type))
            return (RuntimeIshtarClass*)type;

        var t = frame->method->Owner->Owner->FindType(q, true, false);

        if (t->IsUnresolved)
        {
            frame->vm->FastFail(WNE.MISSING_TYPE, $"Cannot find '{q->NameWithNS}' bulitin type", frame);
            return null;
        }
        
        return (RuntimeIshtarClass*)(_cache[(nint)q] = (nint)t);
    }
}
