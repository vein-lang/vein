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
            return null;
        }

        public static void InitTable(Dictionary<string, RuntimeIshtarMethod> table)
        {
            new RuntimeIshtarMethod("getHalfNaN", Public | Static | Extern)
                .AsNative((delegate*<CallFrame, IshtarObject**, IshtarObject*>)&getHalfNaN)
                .AddInto(table, x => x.Name);
            new RuntimeIshtarMethod("getFloatNaN", Public | Static | Extern)
                .AsNative((delegate*<CallFrame, IshtarObject**, IshtarObject*>)&getHalfNaN)
                .AddInto(table, x => x.Name);
            new RuntimeIshtarMethod("getDecimalNaN", Public | Static | Extern)
                .AsNative((delegate*<CallFrame, IshtarObject**, IshtarObject*>)&getHalfNaN)
                .AddInto(table, x => x.Name);
            new RuntimeIshtarMethod("getDoubleNaN", Public | Static | Extern)
                .AsNative((delegate*<CallFrame, IshtarObject**, IshtarObject*>)&getHalfNaN)
                .AddInto(table, x => x.Name);
            new RuntimeIshtarMethod("getHalfInfinity", Public | Static | Extern)
                .AsNative((delegate*<CallFrame, IshtarObject**, IshtarObject*>)&getHalfNaN)
                .AddInto(table, x => x.Name);
            new RuntimeIshtarMethod("getFloatInfinity", Public | Static | Extern)
                .AsNative((delegate*<CallFrame, IshtarObject**, IshtarObject*>)&getHalfNaN)
                .AddInto(table, x => x.Name);
            new RuntimeIshtarMethod("getDecimalInfinity", Public | Static | Extern)
                .AsNative((delegate*<CallFrame, IshtarObject**, IshtarObject*>)&getHalfNaN)
                .AddInto(table, x => x.Name);
            new RuntimeIshtarMethod("getDoubleInfinity", Public | Static | Extern)
                .AsNative((delegate*<CallFrame, IshtarObject**, IshtarObject*>)&getHalfNaN)
                .AddInto(table, x => x.Name);
            new RuntimeIshtarMethod("getHalfNegativeInfinity", Public | Static | Extern)
                .AsNative((delegate*<CallFrame, IshtarObject**, IshtarObject*>)&getHalfNaN)
                .AddInto(table, x => x.Name);
            new RuntimeIshtarMethod("getFloatNegativeInfinity", Public | Static | Extern)
                .AsNative((delegate*<CallFrame, IshtarObject**, IshtarObject*>)&getHalfNaN)
                .AddInto(table, x => x.Name);
            new RuntimeIshtarMethod("getDecimalNegativeInfinity", Public | Static | Extern)
                .AsNative((delegate*<CallFrame, IshtarObject**, IshtarObject*>)&getHalfNaN)
                .AddInto(table, x => x.Name);
            new RuntimeIshtarMethod("getDoubleNegativeInfinity", Public | Static | Extern)
                .AsNative((delegate*<CallFrame, IshtarObject**, IshtarObject*>)&getHalfNaN)
                .AddInto(table, x => x.Name);
        }
    }
}
