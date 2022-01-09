namespace ishtar;

using System.Diagnostics;
using vein.runtime;
using static vein.runtime.MethodFlags;
public static unsafe class B_Field
{
    [IshtarExport(3, "i_call_Field_setValue")]
    [IshtarExportFlags(Public | Static)]
    public static IshtarObject* FieldSetValue(CallFrame current, IshtarObject** args)
    {
        var arg1 = args[0];
        var arg2 = args[1];
        var arg3 = args[2];

        current.ThrowException(KnowTypes.PlatformIsNotSupportFault(current), "i_call_Field_setValue currently is not support.");
        
        return null;
    }

    [IshtarExport(3, "i_call_Field_getValue")]
    [IshtarExportFlags(Public | Static)]
    public static IshtarObject* FieldGetValue(CallFrame current, IshtarObject** args)
    {
        var arg1 = args[0];
        var arg2 = args[1];

        current.ThrowException(KnowTypes.PlatformIsNotSupportFault(current), "i_call_Field_getValue currently is not support.");
        
        return null;
    }


    internal static IshtarMetaClass ThisClass => IshtarMetaClass.Define("vein/lang", "Field");


    public static void InitTable(Dictionary<string, RuntimeIshtarMethod> table)
    {
        new RuntimeIshtarMethod("i_call_Field_setValue", Public | Static | Extern,
                new VeinArgumentRef("targetObject", VeinCore.ObjectClass),
                new VeinArgumentRef("f", ThisClass),
                new VeinArgumentRef("value", VeinCore.ObjectClass))
            .AsNative((delegate*<CallFrame, IshtarObject**, IshtarObject*>)&FieldSetValue)
            .AddInto(table, x => x.Name);
        new RuntimeIshtarMethod("i_call_Field_getValue", Public | Static | Extern,
                new VeinArgumentRef("targetObject", VeinCore.ObjectClass),
                new VeinArgumentRef("f", ThisClass))
            .AsNative((delegate*<CallFrame, IshtarObject**, IshtarObject*>)&FieldGetValue)
            .AddInto(table, x => x.Name);
    }
}