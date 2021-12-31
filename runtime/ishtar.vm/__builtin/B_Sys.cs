namespace ishtar;

using System.Diagnostics;
using vein.runtime;
using static vein.runtime.MethodFlags;
public static unsafe class B_Sys
{
    [IshtarExport(1, "@value2string")]
    [IshtarExportFlags(Public | Static)]
    public static IshtarObject* ValueToString(CallFrame current, IshtarObject** args)
    {
        var arg1 = args[0];

        FFI.StaticValidate(current, &arg1);
        var @class = arg1->decodeClass();

        return IshtarMarshal.ToIshtarString(arg1, current);
    }

    [IshtarExport(1, "@object2string")]
    [IshtarExportFlags(Public | Static)]
    public static IshtarObject* ObjectToString(CallFrame current, IshtarObject** args)
    {
        var arg1 = args[0];

        FFI.StaticValidate(current, &arg1);
        var @class = arg1->decodeClass();

        return IshtarMarshal.ToIshtarString(arg1, current);
    }

    [IshtarExport(0, "@queryPerformanceCounter")]
    [IshtarExportFlags(Public | Static)]
    public static IshtarObject* QueryPerformanceCounter(CallFrame current, IshtarObject** _)
        => IshtarMarshal.ToIshtarObject(Stopwatch.GetTimestamp(), current);

    public static void InitTable(Dictionary<string, RuntimeIshtarMethod> table)
    {
        new RuntimeIshtarMethod("@value2string", Public | Static | Extern,
                new VeinArgumentRef("value", VeinCore.ValueTypeClass))
            .AsNative((delegate*<CallFrame, IshtarObject**, IshtarObject*>)&ValueToString)
            .AddInto(table, x => x.Name);
        new RuntimeIshtarMethod("@object2string", Public | Static | Extern,
                new VeinArgumentRef("value", VeinCore.ObjectClass))
            .AsNative((delegate*<CallFrame, IshtarObject**, IshtarObject*>)&ObjectToString)
            .AddInto(table, x => x.Name);
        new RuntimeIshtarMethod("@queryPerformanceCounter", Public | Static | Extern)
            .AsNative((delegate*<CallFrame, IshtarObject**, IshtarObject*>)&QueryPerformanceCounter)
            .AddInto(table, x => x.Name);
    }
}
