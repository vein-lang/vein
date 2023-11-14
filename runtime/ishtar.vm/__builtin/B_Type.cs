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
        var vault = current.vm.Vault;
        var gc = current.GetGC();

        ForeignFunctionInterface.StaticValidate(current, &arg1);
        ForeignFunctionInterface.StaticTypeOf(current, &arg1, VeinTypeCode.TYPE_STRING);

        var name = IshtarMarshal.ToDotnetString(arg1, current);

        var results = vault.GlobalFindType(name);
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

        return gc.AllocTypeInfoObject(result, current);
    }

    [IshtarExport(1, "i_call_Type_findField")]
    [IshtarExportFlags(Public | Static)]
    public static IshtarObject* FindField(CallFrame current, IshtarObject** args)
    {
        var arg1 = args[0];
        var arg2 = args[1];

        ForeignFunctionInterface.StaticValidate(current, &arg1);
        ForeignFunctionInterface.StaticValidate(current, &arg2);
        ForeignFunctionInterface.StaticTypeOf(current, &arg2, VeinTypeCode.TYPE_STRING);

        current.ThrowException(KnowTypes.PlatformIsNotSupportFault(current));

        return null;
    }

    internal static IshtarMetaClass ThisClass => IshtarMetaClass.Define("vein/lang", "Type");


    public static void InitTable(ForeignFunctionInterface ffi)
    {
        var table = ffi.method_table;
        ffi.vm.CreateInternalMethod("i_call_Type_findByName", Public | Static | Extern,
                new VeinArgumentRef("name", ffi.vm.Types.StringClass))
            .AsNative((delegate*<CallFrame, IshtarObject**, IshtarObject*>)&FindByName)
            .AddInto(table, x => x.Name);
        ffi.vm.CreateInternalMethod("i_call_Type_findField", Public | Static | Extern,
                new VeinArgumentRef("type", ThisClass), new VeinArgumentRef("name", ffi.vm.Types.StringClass))
            .AsNative((delegate*<CallFrame, IshtarObject**, IshtarObject*>)&FindField)
            .AddInto(table, x => x.Name);
        ffi.vm.CreateInternalMethod("i_call_Type_findMethod", Public | Static | Extern,
                new VeinArgumentRef("type", ThisClass), new VeinArgumentRef("name", ffi.vm.Types.StringClass))
            .AsNative((delegate*<CallFrame, IshtarObject**, IshtarObject*>)&FindField)
            .AddInto(table, x => x.Name);
    }
}
