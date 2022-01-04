namespace ishtar;

using vein.runtime;
using static vein.runtime.MethodFlags;
public static unsafe class B_Type
{
    [IshtarExport(1, "i_call_Type_findByName")]
    [IshtarExportFlags(Public | Static)]
    public static IshtarObject* FindByName(CallFrame current, IshtarObject** args)
    {
        var arg1 = args[0];

        FFI.StaticValidate(current, &arg1);
        FFI.StaticTypeOf(current, &arg1, VeinTypeCode.TYPE_STRING);

        var name = IshtarMarshal.ToDotnetString(arg1, current);

        var results = AppVault.CurrentVault.GlobalFindType(name);
        if (results.Length == 0)
        {
            current.ThrowException(KnowTypes.TypeNotFoundFault(current), $"'{name}' not found.");
            return null;
        }

        if (results.Length > 1)
        {
            // todo, add info about all types
            current.ThrowException(KnowTypes.MultipleTypeFoundFault(current), $"Multiple detected '{name}' types."); 
            return null;
        }

        var result = results[0];

        return IshtarGC.AllocTypeObject(result, current);
    }
    public static void InitTable(Dictionary<string, RuntimeIshtarMethod> table)
    {
        new RuntimeIshtarMethod("i_call_Type_findByName", Public | Static | Extern,
                new VeinArgumentRef("name", VeinCore.StringClass))
            .AsNative((delegate*<CallFrame, IshtarObject**, IshtarObject*>)&FindByName)
            .AddInto(table, x => x.Name);
    }
}
