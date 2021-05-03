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
            //FFI.StaticTypeOf(current, &arg1, TYPE_STRING);
            var @class = arg1->Unpack();

            var p = (StrRef*)@class.vtable[@class.Field["!!value"].vtable_offset];
            p->index = 1;
            var str = StrRef.Unwrap(p);
            
            Out.WriteLine();
            Out.WriteLine($"\t{str}");
            Out.WriteLine();

            return null;
        }

        [IshtarExport(1, "@_readline")]
        [IshtarExportFlags(Public | Static)]
        public static IshtarObject* FReadLine(CallFrame current, IshtarObject** args)
        {
            var result = In.ReadLine();

            var p = StringStorage.Intern(result);
            var v = new stackval
            {
                type = TYPE_STRING, data = { p = (nint) p }
            };

            var obj = IshtarGC.AllocObject(TYPE_STRING.AsRuntimeClass());
            var clazz = obj->Unpack();
            
            obj->vtable[clazz.Field["!!value"].vtable_offset] = &v;
            
            return obj;
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