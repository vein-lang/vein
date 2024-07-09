namespace ishtar;

using vein.runtime;
using static vein.runtime.MethodFlags;
public static unsafe class B_Sys
{
    [IshtarExport(1, "@value2string")]
    [IshtarExportFlags(Public | Static)]
    public static IshtarObject* ValueToString(CallFrame* current, IshtarObject** args)
    {
        var arg1 = args[0];

        ForeignFunctionInterface.StaticValidate(current, &arg1);
        var @class = arg1->clazz;

        return IshtarMarshal.ToIshtarString(arg1, current);
    }

    [IshtarExport(1, "@object2string")]
    [IshtarExportFlags(Public | Static)]
    public static IshtarObject* ObjectToString(CallFrame* current, IshtarObject** args)
    {
        var arg1 = args[0];

        ForeignFunctionInterface.StaticValidate(current, &arg1);
        var @class = arg1->clazz;

        return IshtarMarshal.ToIshtarString(arg1, current);
    }

    [IshtarExport(0, "@queryPerformanceCounter")]
    [IshtarExportFlags(Public | Static)]
    public static IshtarObject* QueryPerformanceCounter(CallFrame* current, IshtarObject** _)
        => current->GetGC().ToIshtarObject(Stopwatch.GetTimestamp(), current);

    public static void InitTable(ForeignFunctionInterface ffi)
    {
        //ffi.Add(ffi.vm.CreateInternalMethod("@value2string", Public | Static | Extern,
        //        new VeinArgumentRef("value", ffi.vm.Types->ValueTypeClass))
        //    ->AsNative((delegate*<CallFrame*, IshtarObject**, IshtarObject*>)&ValueToString));
        //ffi.vm.CreateInternalMethod("@object2string", Public | Static | Extern,
        //        new VeinArgumentRef("value", ffi.vm.Types.ObjectClass))
        //    .AsNative((delegate*<CallFrame*, IshtarObject**, IshtarObject*>)&ObjectToString)
        //    .AddInto(table, x => x.Name);
        //ffi.vm.CreateInternalMethod("@queryPerformanceCounter", Public | Static | Extern)
        //    .AsNative((delegate*<CallFrame*, IshtarObject**, IshtarObject*>)&QueryPerformanceCounter)
        //    .AddInto(table, x => x.Name);
    }
}
