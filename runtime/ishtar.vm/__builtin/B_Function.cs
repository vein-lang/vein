namespace ishtar;

using vein.runtime;
using static vein.runtime.MethodFlags;
public static unsafe class B_Function
{
    [IshtarExport(2, "i_call_Function_call")]
    [IshtarExportFlags(Public | Static)]
    public static IshtarObject* Call(CallFrame current, IshtarObject** args)
    {
        current.ThrowException(KnowTypes.PlatformIsNotSupportFault(current), "i_call_Field_setValue currently is not support.");
        
        return null;
    }

    [IshtarExport(4, "i_call_Function_create")]
    [IshtarExportFlags(Public | Static)]
    public static IshtarObject* Create(CallFrame current, IshtarObject** args)
    {
        current.ThrowException(KnowTypes.PlatformIsNotSupportFault(current), "i_call_Field_setValue currently is not support.");
        
        return null;
    }


    internal static IshtarMetaClass ThisClass => IshtarMetaClass.Define("vein/lang", "Function");


    public static void InitTable(Dictionary<string, RuntimeIshtarMethod> table)
    {
        new RuntimeIshtarMethod("i_call_Function_call", Public | Static | Extern,
                new VeinArgumentRef("f", ThisClass),
                new VeinArgumentRef("args", VeinCore.ArrayClass))
            .AsNative((delegate*<CallFrame, IshtarObject**, IshtarObject*>)&Call)
            .AddInto(table, x => x.Name);
        new RuntimeIshtarMethod("i_call_Function_create", Public | Static | Extern,
                new VeinArgumentRef("target", VeinCore.ObjectClass),
                new VeinArgumentRef("name", VeinCore.StringClass),
                new VeinArgumentRef("ignoreCase", VeinCore.BoolClass),
                new VeinArgumentRef("throwWhenFailBind", VeinCore.BoolClass)
                )
            .AsNative((delegate*<CallFrame, IshtarObject**, IshtarObject*>)&Create)
            .AddInto(table, x => x.Name);
    }
}
