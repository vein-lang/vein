namespace ishtar
{
    using System.Collections.Generic;
    using System.Runtime.InteropServices;
    using wave.runtime;
    using static wave.runtime.MethodFlags;
    using static wave.runtime.WaveTypeCode;

    public static unsafe class FE_Application
    {
        [IshtarExport(0, "@_get_os_value")]
        [IshtarExportFlags(Public | Static)]
        public static IshtarObject* GetOSValue(CallFrame current, IshtarObject** args)
        {
            // TODO remove using RuntimeInformation
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) 
                return IshtarGC.AllocInt(0);
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                return IshtarGC.AllocInt(1);
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                return IshtarGC.AllocInt(2);
            return IshtarGC.AllocInt(-1);
        }


        [IshtarExport(1, "@_exit")]
        [IshtarExportFlags(Public | Static)]
        public static IshtarObject* Exit(CallFrame current, IshtarObject** args)
        {
            var exitCode = args[0];
            
            FFI.StaticValidate(current, &exitCode);
            FFI.StaticTypeOf(current, &exitCode, TYPE_I4);
            FFI.StaticValidateField(current, &exitCode, "!!value");

            var clazz = exitCode->DecodeClass();
            
            VM.shutdown((int)(int*)exitCode->vtable[clazz.Field["!!value"].vtable_offset]);

            return null;
        }

        public static void InitTable(Dictionary<string, RuntimeIshtarMethod> table)
        {
            new RuntimeIshtarMethod("@_get_os_value", Public | Static | Extern)
                .AsNative((delegate*<CallFrame, IshtarObject**, IshtarObject*>)&GetOSValue)
                .AddInto(table, x => x.Name);

            new RuntimeIshtarMethod("@_exit", Public | Static | Extern, TYPE_I4.AsClass())
                .AsNative((delegate*<CallFrame, IshtarObject**, IshtarObject*>)&Exit)
                .AddInto(table, x => x.Name);
        }
    }
}