namespace ishtar;

using static vein.runtime.MethodFlags;

public static unsafe class B_GC
{
    [IshtarExport(0, "i_call_GC_get_allocated")]
    [IshtarExportFlags(Public | Static)]
    public static IshtarObject* GetAllocatedBytes(CallFrame* current, IshtarObject** _)
        => current->GetGC().ToIshtarObject(current->vm.GC.Stats.total_allocations, current);

    [IshtarExport(0, "i_call_GC_get_alive_objects")]
    [IshtarExportFlags(Public | Static)]
    public static IshtarObject* GetAliveObjects(CallFrame* current, IshtarObject** _)
        => current->GetGC().ToIshtarObjectT(current->vm.GC.Stats.alive_objects, current);

    public static void InitTable(ForeignFunctionInterface ffi)
    {
        ffi.Add("i_call_GC_get_allocated", Public | Static | Extern)->
            AsNative((delegate*<CallFrame*, IshtarObject**, IshtarObject*>)&GetAllocatedBytes);

        ffi.Add("i_call_GC_get_alive_objects", Public | Static | Extern)->
            AsNative((delegate*<CallFrame*, IshtarObject**, IshtarObject*>)&GetAliveObjects);
    }
}
