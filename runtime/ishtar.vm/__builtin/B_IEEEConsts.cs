namespace ishtar
{
    using static vein.runtime.MethodFlags;
    using static vein.runtime.VeinTypeCode;

    public static unsafe class B_IEEEConsts
    {
        [IshtarExport(0, "getHalfNaN")]
        [IshtarExportFlags(Public | Static)]
        public static IshtarObject* getHalfNaN(CallFrame* current, IshtarObject** args)
        {
            current->vm->FastFail(WNE.MISSING_METHOD, "[B_IEEEConsts::getHalfNaN]", current);
            return null;
        }

        public static void InitTable(ForeignFunctionInterface ffi)
        {
            ffi.Add("i_call_get_Half_NaN", Public | Static | Extern, TYPE_R2)
                ->AsNative((delegate*<CallFrame*, IshtarObject**, IshtarObject*>)&getHalfNaN);

            ffi.Add("i_call_get_Float_NaN", Public | Static | Extern, TYPE_R4)
                ->AsNative((delegate*<CallFrame*, IshtarObject**, IshtarObject*>)&getHalfNaN);

            ffi.Add("i_call_get_Decimal_NaN", Public | Static | Extern, TYPE_R16)
                    ->AsNative((delegate*<CallFrame*, IshtarObject**, IshtarObject*>)&getHalfNaN);

            ffi.Add("i_call_get_Double_NaN", Public | Static | Extern, TYPE_R8)
                ->AsNative((delegate*<CallFrame*, IshtarObject**, IshtarObject*>)&getHalfNaN);

            ffi.Add("i_call_get_Half_Infinity", Public | Static | Extern, TYPE_R2)
                    ->AsNative((delegate*<CallFrame*, IshtarObject**, IshtarObject*>)&getHalfNaN);

            ffi.Add("i_call_get_Float_Infinity", Public | Static | Extern, TYPE_R4)
                ->AsNative((delegate*<CallFrame*, IshtarObject**, IshtarObject*>)&getHalfNaN);

            ffi.Add("i_call_get_Decimal_Infinity", Public | Static | Extern, TYPE_R16)
                ->AsNative((delegate*<CallFrame*, IshtarObject**, IshtarObject*>)&getHalfNaN);

            ffi.Add("i_call_get_Double_Infinity", Public | Static | Extern, TYPE_R8)
                ->AsNative((delegate*<CallFrame*, IshtarObject**, IshtarObject*>)&getHalfNaN);

            ffi.Add("i_call_get_Half_NegativeInfinity", Public | Static | Extern, TYPE_R2)
                ->AsNative((delegate*<CallFrame*, IshtarObject**, IshtarObject*>)&getHalfNaN);

            ffi.Add("i_call_get_Float_NegativeInfinity", Public | Static | Extern, TYPE_R4)
                ->AsNative((delegate*<CallFrame*, IshtarObject**, IshtarObject*>)&getHalfNaN);

            ffi.Add("i_call_get_Decimal_NegativeInfinity", Public | Static | Extern, TYPE_R16)
                ->AsNative((delegate*<CallFrame*, IshtarObject**, IshtarObject*>)&getHalfNaN);

            ffi.Add("i_call_get_Double_NegativeInfinity", Public | Static | Extern, TYPE_R8)
                ->AsNative((delegate*<CallFrame*, IshtarObject**, IshtarObject*>)&getHalfNaN);

        }
    }
}
