namespace ishtar;

using vein.runtime;
using static vein.runtime.MethodFlags;

public unsafe static class B_StringBuilder
{
    [IshtarExport(2, "i_call_StringBuilder_append")]
    [IshtarExportFlags(Public | Static)]
    public static IshtarObject* Append(CallFrame current, IshtarObject** args)
    {
        var arg1 = args[0];
        var arg2 = args[1];

        FFI.StaticValidate(current, &arg1);
        var @class = arg1->decodeClass();

        return IshtarMarshal.ToIshtarString(arg1, current);
    }
    [IshtarExport(2, "i_call_StringBuilder_appendLine")]
    [IshtarExportFlags(Public | Static)]
    public static IshtarObject* AppendLine(CallFrame current, IshtarObject** args)
    {
        var arg1 = args[0];
        var arg2 = args[1];

        FFI.StaticValidate(current, &arg1);
        var @class = arg1->decodeClass();

        return IshtarMarshal.ToIshtarString(arg1, current);
    }

    internal static IshtarMetaClass ThisClass => IshtarMetaClass.Define("vein/lang", "StringBuilder");

    public static void InitTable(Dictionary<string, RuntimeIshtarMethod> table)
    {
        new RuntimeIshtarMethod("i_call_StringBuilder_append", Public | Static | Extern,
                new VeinArgumentRef("_this_", ThisClass), new VeinArgumentRef("value", VeinCore.ObjectClass))
            .AsNative((delegate*<CallFrame, IshtarObject**, IshtarObject*>)&Append)
            .AddInto(table, x => x.Name);
        new RuntimeIshtarMethod("i_call_StringBuilder_append", Public | Static | Extern,
                new VeinArgumentRef("_this_", ThisClass), new VeinArgumentRef("value", VeinCore.ValueTypeClass))
            .AsNative((delegate*<CallFrame, IshtarObject**, IshtarObject*>)&Append)
            .AddInto(table, x => x.Name);

        new RuntimeIshtarMethod("i_call_StringBuilder_appendLine", Public | Static | Extern,
                new VeinArgumentRef("_this_", ThisClass), new VeinArgumentRef("value", VeinCore.ObjectClass))
            .AsNative((delegate*<CallFrame, IshtarObject**, IshtarObject*>)&AppendLine)
            .AddInto(table, x => x.Name);
        new RuntimeIshtarMethod("i_call_StringBuilder_appendLine", Public | Static | Extern,
                new VeinArgumentRef("_this_", ThisClass), new VeinArgumentRef("value", VeinCore.ValueTypeClass))
            .AsNative((delegate*<CallFrame, IshtarObject**, IshtarObject*>)&AppendLine)
            .AddInto(table, x => x.Name);
    }
}
