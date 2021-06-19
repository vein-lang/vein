namespace ishtar
{
    using System.Collections.Generic;
    using System.Runtime.InteropServices;
    using static mana.runtime.MethodFlags;
    using static mana.runtime.ManaTypeCode;

    public static unsafe class B_App
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

            VM.halt(IshtarMarshal.ToDotnetInt32(exitCode, current));

            return null;
        }

        [IshtarExport(2, "@_switch_flag")]
        [IshtarExportFlags(Public | Static)]
        public static IshtarObject* SwitchFlag(CallFrame current, IshtarObject** args)
        {
            var key = args[0];
            var value = args[1];

            FFI.StaticValidate(current, &key);
            FFI.StaticTypeOf(current, &key, TYPE_STRING);
            FFI.StaticValidate(current, &value);
            FFI.StaticTypeOf(current, &value, TYPE_BOOLEAN);

            FFI.StaticValidateField(current, &key, "!!value");
            FFI.StaticValidateField(current, &value, "!!value");

            var clr_key = IshtarMarshal.ToDotnetString(key, current);
            var clr_value = IshtarMarshal.ToDotnetBoolean(value, current);

            VM.Config.Set(clr_key, clr_value);

            return null;
        }

        public static void InitTable(Dictionary<string, RuntimeIshtarMethod> table)
        {
            new RuntimeIshtarMethod("@_get_os_value", Public | Static | Extern)
                .AsNative((delegate*<CallFrame, IshtarObject**, IshtarObject*>)&GetOSValue)
                .AddInto(table, x => x.Name);

            new RuntimeIshtarMethod("@_exit", Public | Static | Extern, (TYPE_STRING, "msg"), (TYPE_I4, "code"))
                .AsNative((delegate*<CallFrame, IshtarObject**, IshtarObject*>)&Exit)
                .AddInto(table, x => x.Name);

            new RuntimeIshtarMethod("@_switch_flag", Public | Static | Extern, (TYPE_STRING, "key"), (TYPE_BOOLEAN, "value"))
                .AsNative((delegate*<CallFrame, IshtarObject**, IshtarObject*>)&SwitchFlag)
                .AddInto(table, x => x.Name);
        }
    }
}
