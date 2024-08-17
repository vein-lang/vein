namespace ishtar;

using static vein.runtime.MethodFlags;
using static vein.runtime.VeinTypeCode;

public static unsafe class B_GC
{
    [IshtarExport(0, "i_call_GC_get_allocated")]
    [IshtarExportFlags(Public | Static)]
    public static IshtarObject* GetAllocatedBytes(CallFrame* current, IshtarObject** _)
        => current->GetGC()->ToIshtarObject(current->vm->gc->total_allocations, current);

    [IshtarExport(0, "i_call_GC_get_alive_objects")]
    [IshtarExportFlags(Public | Static)]
    public static IshtarObject* GetAliveObjects(CallFrame* current, IshtarObject** _)
        => current->GetGC()->ToIshtarObjectT(current->vm->gc->alive_objects, current);


    private static IshtarObject* AllocateBlock(CallFrame* current, IshtarObject** args)
    {
        var span = new Vein_Span_u8(args[0]);
        var bytes = current->vm->gc->AllocSpanTable(current, (uint)span._length);
        span._ptr = bytes;
        return null;
    }

    private static IshtarObject* DeAllocateBlock(CallFrame* current, IshtarObject** args)
    {
        var span = new Vein_Span_u8(args[0]);
        current->vm->gc->FreeSpanTable(current, span._ptr);
        return null;
    }

    public static void InitTable(ForeignFunctionInterface ffi)
    {
        ffi.Add("alloc_block([std]::std::Object) -> [std]::std::Void", ffi.AsNative(&AllocateBlock));
        ffi.Add("dealloc_block([std]::std::Raw) -> [std]::std::Void", ffi.AsNative(&DeAllocateBlock));


        ffi.Add("i_call_GC_get_allocated", Public | Static | Extern, TYPE_I8)->
            AsNative((delegate*<CallFrame*, IshtarObject**, IshtarObject*>)&GetAllocatedBytes);

        ffi.Add("i_call_GC_get_alive_objects", Public | Static | Extern, TYPE_I8)->
            AsNative((delegate*<CallFrame*, IshtarObject**, IshtarObject*>)&GetAliveObjects);
    }
}
