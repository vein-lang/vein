namespace ishtar;

using vein.runtime;
using static vein.runtime.MethodFlags;
public static unsafe class B_Function
{
    [IshtarExport(2, "i_call_Function_call")]
    [IshtarExportFlags(Public | Static)]
    public static IshtarObject* Call(CallFrame* current, IshtarObject** args)
    {
       // current.ThrowException(KnowTypes.PlatformIsNotSupportFault(current), "i_call_Field_setValue currently is not support.");

        return null;
    }

    [IshtarExport(4, "i_call_Function_create")]
    [IshtarExportFlags(Public | Static)]
    public static IshtarObject* Create(CallFrame* current, IshtarObject** args)
    {
        //current.ThrowException(KnowTypes.PlatformIsNotSupportFault(current), "i_call_Field_setValue currently is not support.");

        return null;
    }


    public static void InitTable(ForeignFunctionInterface ffi)
    {
        //var table = ffi.method_table;
        //ffi.vm->CreateInternalMethod("i_call_Function_call", Public | Static | Extern,
        //        new VeinArgumentRef("f", ThisClass),
        //        new VeinArgumentRef("args", ffi.vm->Types.ArrayClass))
        //    .AsNative((delegate*<CallFrame, IshtarObject**, IshtarObject*>)&Call)
        //    .AddInto(table, x => x.Name);
        //ffi.vm->CreateInternalMethod("i_call_Function_create", Public | Static | Extern,
        //        new VeinArgumentRef("target", ffi.vm->Types.ObjectClass),
        //        new VeinArgumentRef("name", ffi.vm->Types.StringClass),
        //        new VeinArgumentRef("ignoreCase", ffi.vm->Types.BoolClass),
        //        new VeinArgumentRef("throwWhenFailBind", ffi.vm->Types.BoolClass)
        //        )
        //    .AsNative((delegate*<CallFrame, IshtarObject**, IshtarObject*>)&Create)
        //    .AddInto(table, x => x.Name);
    }
}
