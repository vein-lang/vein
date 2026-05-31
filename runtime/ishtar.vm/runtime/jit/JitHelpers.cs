namespace ishtar.jit;

using System.Runtime.InteropServices;
using ishtar.runtime;
using ishtar.runtime.gc;
using vein.runtime;
using static vein.runtime.VeinTypeCode;

/// <summary>
/// Runtime helper functions callable from JIT-compiled native code.
/// Each helper is [UnmanagedCallersOnly] so it can be invoked directly
/// from emitted x64 machine code without managed transition overhead.
///
/// The JIT embeds these function addresses as immediate constants in generated code.
/// </summary>
public static unsafe class JitHelpers
{
    /// <summary>
    /// Cached function pointers for all helpers. Populated once at startup.
    /// </summary>
    public static JitHelpersTable Table;

    public static void Initialize()
    {
        Table = new JitHelpersTable
        {
            InitStruct = (nint)(delegate* unmanaged<RuntimeIshtarClass*, CallFrame*, IshtarObject*>)&Helper_InitStruct,
            CopyStruct = (nint)(delegate* unmanaged<IshtarObject*, RuntimeIshtarClass*, CallFrame*, IshtarObject*>)&Helper_CopyStruct,
            StoreField = (nint)(delegate* unmanaged<IshtarObject*, RuntimeIshtarField*, stackval*, CallFrame*, void>)&Helper_StoreField,
            LoadField = (nint)(delegate* unmanaged<IshtarObject*, RuntimeIshtarField*, CallFrame*, stackval*, void>)&Helper_LoadField,
            Box = (nint)(delegate* unmanaged<stackval*, RuntimeIshtarClass*, CallFrame*, IshtarObject*>)&Helper_Box,
            Unbox = (nint)(delegate* unmanaged<IshtarObject*, RuntimeIshtarClass*, CallFrame*, stackval*, void>)&Helper_Unbox
        };
    }

    /// <summary>
    /// INITSTRUCT: Allocate and zero-initialize a struct object.
    /// </summary>
    [UnmanagedCallersOnly]
    public static IshtarObject* Helper_InitStruct(RuntimeIshtarClass* clazz, CallFrame* frame)
        => frame->vm->gc->AllocObject(clazz, frame);

    /// <summary>
    /// CPSTRUCT: Deep-copy a struct object (allocate new + copy vtable contents).
    /// </summary>
    [UnmanagedCallersOnly]
    public static IshtarObject* Helper_CopyStruct(IshtarObject* src, RuntimeIshtarClass* clazz, CallFrame* frame)
    {
        var copy = frame->vm->gc->AllocObject(clazz, frame);
        IshtarUnsafe.CopyBlock(copy->vtable, src->vtable,
            (uint)clazz->computed_size * (uint)sizeof(void*));
        return copy;
    }

    /// <summary>
    /// STF: Store a value into an object field (boxes primitive values).
    /// </summary>
    [UnmanagedCallersOnly]
    public static void Helper_StoreField(IshtarObject* obj, RuntimeIshtarField* field, stackval* value, CallFrame* frame)
    {
        if (value->type == TYPE_NULL)
            obj->vtable[field->vtable_offset] = null;
        else if (value->type == TYPE_RAW)
            obj->vtable[field->vtable_offset] = (void*)value->data.p;
        else
            obj->vtable[field->vtable_offset] = IshtarMarshal.Boxing(frame, value);
    }

    /// <summary>
    /// LDF: Load a value from an object field (unboxes to primitive).
    /// Result written to output stackval pointer.
    /// </summary>
    [UnmanagedCallersOnly]
    public static void Helper_LoadField(IshtarObject* obj, RuntimeIshtarField* field, CallFrame* frame, stackval* result)
    {
        var raw = (IshtarObject*)obj->vtable[field->vtable_offset];
        if (field->FieldType.Class->TypeCode is TYPE_RAW)
        {
            result->type = TYPE_RAW;
            result->data.p = (nint)raw;
        }
        else
        {
            *result = IshtarMarshal.UnBoxing(frame, raw);
        }
    }

    /// <summary>
    /// BOX: Box a value type into a heap-allocated object.
    /// Stores raw value bits directly into vtable slot (no intermediate allocation).
    /// </summary>
    [UnmanagedCallersOnly]
    public static IshtarObject* Helper_Box(stackval* value, RuntimeIshtarClass* clazz, CallFrame* frame)
    {
        var boxed = frame->vm->gc->AllocObject(clazz, frame);
        var valField = clazz->FindField("!!value");
        if (valField is not null)
        {
            if (value->type == TYPE_R16)
                boxed->vtable[valField->vtable_offset] = IshtarMarshal.Boxing(frame, value);
            else
                boxed->vtable[valField->vtable_offset] = (void*)value->data.p;
        }
        return boxed;
    }

    /// <summary>
    /// UNBOX: Extract a value from a boxed object.
    /// Reads raw value bits directly from vtable slot (no intermediate UnBoxing).
    /// Result written to output stackval pointer.
    /// </summary>
    [UnmanagedCallersOnly]
    public static void Helper_Unbox(IshtarObject* obj, RuntimeIshtarClass* clazz, CallFrame* frame, stackval* result)
    {
        if (obj == null)
        {
            result->type = TYPE_NULL;
            result->data.p = 0;
            return;
        }

        var unboxField = obj->clazz->FindField("!!value");
        if (unboxField is not null)
        {
            var fieldType = unboxField->FieldType.Class->TypeCode;
            if (fieldType == TYPE_R16)
            {
                var unboxedObj = (IshtarObject*)obj->vtable[unboxField->vtable_offset];
                *result = IshtarMarshal.UnBoxing(frame, unboxedObj);
            }
            else
            {
                result->type = fieldType;
                result->data.p = (nint)obj->vtable[unboxField->vtable_offset];
            }
        }
        else
        {
            result->type = clazz->TypeCode;
            result->data.p = (nint)obj;
        }
    }
}

/// <summary>
/// Table of helper function addresses, embedded as constants in JIT-generated code.
/// </summary>
public struct JitHelpersTable
{
    /// <summary>IshtarObject* InitStruct(RuntimeIshtarClass* clazz, CallFrame* frame)</summary>
    public nint InitStruct;
    /// <summary>IshtarObject* CopyStruct(IshtarObject* src, RuntimeIshtarClass* clazz, CallFrame* frame)</summary>
    public nint CopyStruct;
    /// <summary>void StoreField(IshtarObject* obj, RuntimeIshtarField* field, stackval* value, CallFrame* frame)</summary>
    public nint StoreField;
    /// <summary>void LoadField(IshtarObject* obj, RuntimeIshtarField* field, CallFrame* frame, stackval* result)</summary>
    public nint LoadField;
    /// <summary>IshtarObject* Box(stackval* value, RuntimeIshtarClass* clazz, CallFrame* frame)</summary>
    public nint Box;
    /// <summary>void Unbox(IshtarObject* obj, RuntimeIshtarClass* clazz, CallFrame* frame, stackval* result)</summary>
    public nint Unbox;
}
