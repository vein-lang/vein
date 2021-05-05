namespace ishtar
{
    using System.Collections.Generic;
    using wave.runtime;
    using static System.Console;
    using static wave.runtime.MethodFlags;
    using static wave.runtime.WaveTypeCode;

    public static unsafe class FE_Out
    {
        [IshtarExport(1, "@_println")]
        [IshtarExportFlags(Public | Static)]
        public static IshtarObject* FPrintLn(CallFrame current, IshtarObject** args)
        {
            var arg1 = args[0];

            FFI.StaticValidate(current, &arg1);
            FFI.StaticTypeOf(current, &arg1, TYPE_STRING);
            var @class = arg1->DecodeClass();

            var p = (StrRef*)arg1->vtable[@class.Field["!!value"].vtable_offset];
            var str = StrRef.Unwrap(p);
            
            Out.WriteLine();
            Out.WriteLine($"\t{str}");
            Out.WriteLine();

            return null;
        }

        [IshtarExport(0, "@_readline")]
        [IshtarExportFlags(Public | Static)]
        public static IshtarObject* FReadLine(CallFrame current, IshtarObject** args)
        {
            var result = In.ReadLine();
            fixed (IshtarObject** p = &current._this_)
                return IshtarGC.AllocString(result, p);
        }


        public static void InitTable(Dictionary<string, RuntimeIshtarMethod> table)
        {
            new RuntimeIshtarMethod("@_println", Public | Static | Extern, ("val", TYPE_STRING))
                .AsNative((delegate*<CallFrame, IshtarObject**, IshtarObject*>)&FPrintLn)
                .AddInto(table, x => x.Name);

            new RuntimeIshtarMethod("@_readline", Public | Static | Extern, TYPE_STRING.AsClass())
                .AsNative((delegate*<CallFrame, IshtarObject**, IshtarObject*>)&FReadLine)
                .AddInto(table, x => x.Name);
        }
    }
}