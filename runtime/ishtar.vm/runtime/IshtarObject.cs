namespace ishtar;

[CTypeExport("ishtar_object_t")]

public unsafe struct IshtarObject
{
    public IshtarObject() {}

    public static readonly IshtarObject Null = default;
    public static readonly IshtarObject* NullPointer = null;

    public RuntimeIshtarClass* clazz;
    public void** vtable;
    public GCFlags flags;
    public GCColor color;
    public ulong refs_size;
    public double priority => 1d / Math.Max(1, refs_size);

    public uint vtable_size;

#if DEBUG
    public long __gc_id = -1;

    public const long magic1 = 753;
    public const long magic2 = 472;

    public long m1 = magic1;
    public long m2 = magic2;
    public static Dictionary<long, string> CreationTrace = new();

    public string CreationTraceData => CreationTrace[__gc_id];
#endif


    public bool IsValidObject()
    {
#if DEBUG
        return m1 == magic1 && m2 == magic2;
#endif
        return true;
    }

    public static IshtarObject* IsInstanceOf(CallFrame* frame, IshtarObject* @this, RuntimeIshtarClass* @class)
    {
        if (!@class->is_inited)
            @class->init_vtable(frame->vm);
        if (@this == null)
            return null;
        if (@class->IsInterface)
            return IsInstanceOfByRef(frame, @this, @class);
        return IsAssignableFrom(frame, @class, @this->clazz) ? @this : null;
    }

    public static IshtarObject* IsInstanceOfByRef(CallFrame* frame, IshtarObject* c, RuntimeIshtarClass* @class)
    {
        // temporary cast to\from interface is not support
        frame->ThrowException(KnowTypes.IncorrectCastFault(frame));
        return c;
    }

    public static bool IsAssignableFrom(CallFrame* frame, RuntimeIshtarClass* c1, RuntimeIshtarClass* c2)
    {
        if (!c1->is_inited) c1->init_vtable(frame->vm);
        if (!c2->is_inited) c2->init_vtable(frame->vm);
        // TODO: Array detection
        // TODO: Generic detection
        // TODO: Interfrace detection
        if (c1->FullName == frame->vm.Types->ObjectClass->FullName)
            return true;
        return c1->IsInner(c2);
    }
}
