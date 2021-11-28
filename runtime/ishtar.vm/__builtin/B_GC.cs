namespace ishtar;

using static vein.runtime.MethodFlags;

public static unsafe class B_GC
{
    [IshtarExport(0, "i_call_GC_get_allocated")]
    [IshtarExportFlags(Public | Static)]
    public static IshtarObject* GetAllocatedBytes(CallFrame current, IshtarObject** _)
        => IshtarMarshal.ToIshtarObject(IshtarGC.GCStats.total_allocations, current);

    [IshtarExport(0, "i_call_GC_get_alive_objects")]
    [IshtarExportFlags(Public | Static)]
    public static IshtarObject* GetAliveObjects(CallFrame current, IshtarObject** _)
        => IshtarMarshal.ToIshtarObject(IshtarGC.GCStats.alive_objects, current);

    public static void InitTable(Dictionary<string, RuntimeIshtarMethod> table)
    {
        new RuntimeIshtarMethod("i_call_GC_get_allocated", Public | Static | Extern)
            .AsNative((delegate*<CallFrame, IshtarObject**, IshtarObject*>)&GetAllocatedBytes)
            .AddInto(table, x => x.Name);
        new RuntimeIshtarMethod("i_call_GC_get_alive_objects", Public | Static | Extern)
            .AsNative((delegate*<CallFrame, IshtarObject**, IshtarObject*>)&GetAliveObjects)
            .AddInto(table, x => x.Name);
    }
}
