namespace ishtar
{
    using System.Collections.Generic;
    using vein.runtime;
    using static vein.runtime.MethodFlags;
    using static vein.runtime.VeinTypeCode;
    public static unsafe class B_IEEEConsts
    {
        [IshtarExport(0, "getHalfNaN")]
        [IshtarExportFlags(Public | Static)]
        public static IshtarObject* getHalfNaN(CallFrame current, IshtarObject** args)
        {
            current.vm.FastFail(WNE.MISSING_METHOD, "[B_IEEEConsts::getHalfNaN]", current);
            return null;
        }

        public static void InitTable(ForeignFunctionInterface ffi)
        {
            var table = ffi.method_table;

            ffi.vm.CreateInternalMethod("i_call_get_Half_NaN", Public | Static | Extern)
                .AsNative((delegate*<CallFrame, IshtarObject**, IshtarObject*>)&getHalfNaN)
                .AddInto(table, x => x.Name);
            ffi.vm.CreateInternalMethod("i_call_get_Float_NaN", Public | Static | Extern)
                .AsNative((delegate*<CallFrame, IshtarObject**, IshtarObject*>)&getHalfNaN)
                .AddInto(table, x => x.Name);
            ffi.vm.CreateInternalMethod("i_call_get_Decimal_NaN", Public | Static | Extern)
                .AsNative((delegate*<CallFrame, IshtarObject**, IshtarObject*>)&getHalfNaN)
                .AddInto(table, x => x.Name);
            ffi.vm.CreateInternalMethod("i_call_get_Double_NaN", Public | Static | Extern)
                .AsNative((delegate*<CallFrame, IshtarObject**, IshtarObject*>)&getHalfNaN)
                .AddInto(table, x => x.Name);
            ffi.vm.CreateInternalMethod("i_call_get_Half_Infinity", Public | Static | Extern)
                .AsNative((delegate*<CallFrame, IshtarObject**, IshtarObject*>)&getHalfNaN)
                .AddInto(table, x => x.Name);
            ffi.vm.CreateInternalMethod("i_call_get_Float_Infinity", Public | Static | Extern)
                .AsNative((delegate*<CallFrame, IshtarObject**, IshtarObject*>)&getHalfNaN)
                .AddInto(table, x => x.Name);
            ffi.vm.CreateInternalMethod("i_call_get_Decimal_Infinity", Public | Static | Extern)
                .AsNative((delegate*<CallFrame, IshtarObject**, IshtarObject*>)&getHalfNaN)
                .AddInto(table, x => x.Name);
            ffi.vm.CreateInternalMethod("i_call_get_Double_Infinity", Public | Static | Extern)
                .AsNative((delegate*<CallFrame, IshtarObject**, IshtarObject*>)&getHalfNaN)
                .AddInto(table, x => x.Name);
            ffi.vm.CreateInternalMethod("i_call_get_Half_NegativeInfinity", Public | Static | Extern)
                .AsNative((delegate*<CallFrame, IshtarObject**, IshtarObject*>)&getHalfNaN)
                .AddInto(table, x => x.Name);
            ffi.vm.CreateInternalMethod("i_call_get_Float_NegativeInfinity", Public | Static | Extern)
                .AsNative((delegate*<CallFrame, IshtarObject**, IshtarObject*>)&getHalfNaN)
                .AddInto(table, x => x.Name);
            ffi.vm.CreateInternalMethod("i_call_get_Decimal_NegativeInfinity", Public | Static | Extern)
                .AsNative((delegate*<CallFrame, IshtarObject**, IshtarObject*>)&getHalfNaN)
                .AddInto(table, x => x.Name);
            ffi.vm.CreateInternalMethod("i_call_get_Double_NegativeInfinity", Public | Static | Extern)
                .AsNative((delegate*<CallFrame, IshtarObject**, IshtarObject*>)&getHalfNaN)
                .AddInto(table, x => x.Name);
        }
    }
}
