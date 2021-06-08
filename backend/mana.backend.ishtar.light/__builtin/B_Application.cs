namespace ishtar
{
    using System.Collections.Generic;
    using System.Runtime.InteropServices;
    using mana.runtime;
    using static mana.runtime.MethodFlags;
    using static mana.runtime.ManaTypeCode;

    public static unsafe class B_Application
    {
        [IshtarExport(0, "@_get_os_value")]
        [IshtarExportFlags(Public | Static)]
        public static IshtarObject* GetOSValue(CallFrame current, IshtarObject** args)
        {
            // TODO remove using RuntimeInformation
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return IshtarMarshal.ToIshtarObject(0, current);
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                return IshtarMarshal.ToIshtarObject(1, current);
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                return IshtarMarshal.ToIshtarObject(2, current);
            return IshtarMarshal.ToIshtarObject(-1, current);
        }


        [IshtarExport(1, "@_exit")]
        [IshtarExportFlags(Public | Static)]
        public static IshtarObject* Exit(CallFrame current, IshtarObject** args)
        {
            var exitCode = args[0];

            FFI.StaticValidate(current, &exitCode);
            FFI.StaticTypeOf(current, &exitCode, TYPE_I4);
            FFI.StaticValidateField(current, &exitCode, "!!value");

            VM.shutdown(IshtarMarshal.ToDotnetInt32(exitCode, current));

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
