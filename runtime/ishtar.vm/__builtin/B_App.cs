namespace ishtar
{
    using static vein.runtime.MethodFlags;
    using static vein.runtime.VeinTypeCode;

    public static unsafe class B_App
    {
        [IshtarExport(0, "@_get_os_value")]
        [IshtarExportFlags(Public | Static)]
        public static IshtarObject* GetOSValue(CallFrame* current, IshtarObject** args)
        {
            var gc = current->GetGC();
            // TODO remove using RuntimeInformation
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return gc.ToIshtarObject(0, current);
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                return gc.ToIshtarObject(1, current);
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                return gc.ToIshtarObject(2, current);
            return gc.ToIshtarObject(-1, current);
        }


        [IshtarExport(1, "@_exit")]
        [IshtarExportFlags(Public | Static)]
        public static IshtarObject* Exit(CallFrame* current, IshtarObject** args)
        {
            var exitCode = args[0];

            ForeignFunctionInterface.StaticValidate(current, &exitCode);
            ForeignFunctionInterface.StaticTypeOf(current, &exitCode, TYPE_I4);
            ForeignFunctionInterface.StaticValidateField(current, &exitCode, "!!value");

            current->vm.halt(IshtarMarshal.ToDotnetInt32(exitCode, current));

            return null;
        }

        [IshtarExport(2, "@_switch_flag")]
        [IshtarExportFlags(Public | Static)]
        public static IshtarObject* SwitchFlag(CallFrame* current, IshtarObject** args)
        {
            var key = args[0];
            var value = args[1];

            ForeignFunctionInterface.StaticValidate(current, &key);
            ForeignFunctionInterface.StaticTypeOf(current, &key, TYPE_STRING);
            ForeignFunctionInterface.StaticValidate(current, &value);
            ForeignFunctionInterface.StaticTypeOf(current, &value, TYPE_BOOLEAN);

            ForeignFunctionInterface.StaticValidateField(current, &key, "!!value");
            ForeignFunctionInterface.StaticValidateField(current, &value, "!!value");

            var clr_key = IshtarMarshal.ToDotnetString(key, current);
            var clr_value = IshtarMarshal.ToDotnetBoolean(value, current);

            current->vm.Config.Set(clr_key, clr_value);

            return null;
        }

        public static void InitTable(ForeignFunctionInterface ffi)
        {
            ffi.Add("@_get_os_value", Public | Static | Extern, TYPE_I4)->
                AsNative((delegate*<CallFrame*, IshtarObject**, IshtarObject*>)&GetOSValue);

            ffi.Add("@_exit", Public | Static | Extern, TYPE_VOID, ("msg", TYPE_STRING), ("code", TYPE_I4))->
                AsNative((delegate*<CallFrame*, IshtarObject**, IshtarObject*>)&Exit);

            ffi.Add("@_switch_flag", Public | Static | Extern, TYPE_VOID, ("key", TYPE_STRING), ("value", TYPE_BOOLEAN))
                ->AsNative((delegate*<CallFrame*, IshtarObject**, IshtarObject*>)&SwitchFlag);
        }
    }
}
