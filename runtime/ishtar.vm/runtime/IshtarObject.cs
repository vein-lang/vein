namespace ishtar;

using LLVMSharp;
using vein.runtime;

[CTypeExport("ishtar_object_t")]

public unsafe struct IshtarObject
{
    public IshtarObject() {}


    public static readonly IshtarObject Null = default;
    public static readonly IshtarObject* NullPointer = null;

    public RuntimeIshtarClass* clazz
    {
        get => _class;
        set
        {
            if (_class is not null)
                throw new InvalidOperationException();
            if (value is null)
                throw new InvalidOperationException();
            _class = value;
        }
    }

    private void* reserved1;
    private void* reserved2;
    public void** vtable;
    public GCFlags flags;
    public RuntimeIshtarClass* _class;
    public uint vtable_size;

#if DEBUG
    public long __gc_id = -1;

    public const long magic1 = 753;
    public const long magic2 = 472;

    public long m1 = magic1;
    public long m2 = magic2;

    public static readonly Dictionary<long, string> CreationTrace = new();
    public static Dictionary<long, string> debug_names_allocation = new Dictionary<long, string>();
    public string CreationTraceData => CreationTrace[__gc_id];
    public string ClassTraceData => debug_names_allocation[__gc_id];
#endif

    public bool IsDestroyedObject()
        => m1 == 0 && m2 == 0 && flags == 0 && vtable == null;

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
        if (c1->FullName == frame->vm->Types->ObjectClass->FullName)
            return true;
        return c1->IsInner(c2);
    }
}


public static unsafe class IshtarObjectEx
{
    public static byte GetUInt8(this IshtarObject value)
    {
        VirtualMachine.Assert(value.clazz->TypeCode == VeinTypeCode.TYPE_U1, WNE.TYPE_MISMATCH, "", &value);

        return (byte)value.vtable[value.clazz->Field["!!value"]->vtable_offset];
    }
    public static ushort GetUInt16(this IshtarObject value)
    {
        VirtualMachine.Assert(value.clazz->TypeCode == VeinTypeCode.TYPE_U2, WNE.TYPE_MISMATCH, "", &value);

        return (ushort)value.vtable[value.clazz->Field["!!value"]->vtable_offset];
    }
    public static uint GetUInt32(this IshtarObject value)
    {
        VirtualMachine.Assert(value.clazz->TypeCode == VeinTypeCode.TYPE_U4, WNE.TYPE_MISMATCH, "", &value);

        return (uint)value.vtable[value.clazz->Field["!!value"]->vtable_offset];
    }
    public static short GetInt16(this IshtarObject value)
    {
        VirtualMachine.Assert(value.clazz->TypeCode == VeinTypeCode.TYPE_I2, WNE.TYPE_MISMATCH, "", &value);

        return (short)value.vtable[value.clazz->Field["!!value"]->vtable_offset];
    }
    public static int GetInt32(this IshtarObject value)
    {
        VirtualMachine.Assert(value.clazz->TypeCode == VeinTypeCode.TYPE_I4, WNE.TYPE_MISMATCH, "", &value);

        return (int)value.vtable[value.clazz->Field["!!value"]->vtable_offset];
    }

    public static ulong GetUInt64(this IshtarObject value)
    {
        VirtualMachine.Assert(value.clazz->TypeCode == VeinTypeCode.TYPE_U8, WNE.TYPE_MISMATCH, "", &value);

        return (ulong)value.vtable[value.clazz->Field["!!value"]->vtable_offset];
    }
    public static long GetInt64(this IshtarObject value)
    {
        VirtualMachine.Assert(value.clazz->TypeCode == VeinTypeCode.TYPE_I8, WNE.TYPE_MISMATCH, "", &value);

        return (long)value.vtable[value.clazz->Field["!!value"]->vtable_offset];
    }


    public static void SetUInt8(this IshtarObject value, byte v)
    {
        VirtualMachine.Assert(value.clazz->TypeCode == VeinTypeCode.TYPE_U1, WNE.TYPE_MISMATCH, "", &value);

        value.vtable[value.clazz->Field["!!value"]->vtable_offset] = (void*)v;
    }
    public static void SetUInt16(this IshtarObject value, ushort v)
    {
        VirtualMachine.Assert(value.clazz->TypeCode == VeinTypeCode.TYPE_U2, WNE.TYPE_MISMATCH, "", &value);

        value.vtable[value.clazz->Field["!!value"]->vtable_offset] = (void*)v;
    }
    public static void SetUInt32(this IshtarObject value, uint v)
    {
        VirtualMachine.Assert(value.clazz->TypeCode == VeinTypeCode.TYPE_U4, WNE.TYPE_MISMATCH, "", &value);

        value.vtable[value.clazz->Field["!!value"]->vtable_offset] = (void*)v;
    }
    public static void SetUInt64(this IshtarObject value, ulong v)
    {
        VirtualMachine.Assert(value.clazz->TypeCode == VeinTypeCode.TYPE_U8, WNE.TYPE_MISMATCH, "", &value);

        value.vtable[value.clazz->Field["!!value"]->vtable_offset] = (void*)v;
    }

    public static void SetInt16(this IshtarObject value, short v)
    {
        VirtualMachine.Assert(value.clazz->TypeCode == VeinTypeCode.TYPE_I2, WNE.TYPE_MISMATCH, "", &value);

        value.vtable[value.clazz->Field["!!value"]->vtable_offset] = (void*)v;
    }
    public static void SetInt32(this IshtarObject value, int v)
    {
        VirtualMachine.Assert(value.clazz->TypeCode == VeinTypeCode.TYPE_I4, WNE.TYPE_MISMATCH, "", &value);

        value.vtable[value.clazz->Field["!!value"]->vtable_offset] = (void*)v;
    }
    public static void SetInt64(this IshtarObject value, long v)
    {
        VirtualMachine.Assert(value.clazz->TypeCode == VeinTypeCode.TYPE_I8, WNE.TYPE_MISMATCH, "", &value);

        value.vtable[value.clazz->Field["!!value"]->vtable_offset] = (void*)v;
    }
}
