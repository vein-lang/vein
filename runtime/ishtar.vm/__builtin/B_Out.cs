namespace ishtar
{
    using static Console;
    using static vein.runtime.MethodFlags;
    using static vein.runtime.VeinTypeCode;

    public static unsafe class B_Out
    {
        [IshtarExport(1, "@_println")]
        [IshtarExportFlags(Public | Static)]
        public static IshtarObject* FPrintLn_Object(CallFrame* current, IshtarObject** args)
        {
            var arg1 = args[0];

            if (arg1 == null)
            {
                current->ThrowException(KnowTypes.NullPointerException(current));
                return null;
            }

            ForeignFunctionInterface.StaticValidate(current, &arg1);
            var @class = arg1->clazz;

            var str = @class->TypeCode is TYPE_STRING ?
                    IshtarMarshal.ToDotnetString(arg1, current) :
                    IshtarMarshal.ToDotnetString(IshtarMarshal.ToIshtarString(arg1, current), current);

            current->vm->trace.console_std_write_line(str);
            return null;
        }

        public static IshtarObject* FPrint_Object(CallFrame* current, IshtarObject** args)
        {
            var arg1 = args[0];

            if (arg1 == null)
            {
                current->ThrowException(KnowTypes.NullPointerException(current));
                return null;
            }

            ForeignFunctionInterface.StaticValidate(current, &arg1);
            var @class = arg1->clazz;

            var str = @class->TypeCode is TYPE_STRING ?
                IshtarMarshal.ToDotnetString(arg1, current) :
                IshtarMarshal.ToDotnetString(IshtarMarshal.ToIshtarString(arg1, current), current);

            current->vm->trace.console_std_write(str);
            return null;
        }

        [IshtarExport(0, "@_readline")]
        [IshtarExportFlags(Public | Static)]
        public static IshtarObject* FReadLine(CallFrame* current, IshtarObject** args)
            => current->GetGC()->ToIshtarObject(In.ReadLine(), current);


        public static void InitTable(ForeignFunctionInterface ffi)
        {
            ffi.Add("print_ln_any([std]::std::Object) -> [std]::std::Void", ffi.AsNative(&FPrintLn_Object));
            ffi.Add("print_any([std]::std::Object) -> [std]::std::Void", ffi.AsNative(&FPrint_Object));
            ffi.Add("@_readline", Public | Static | Extern,  TYPE_STRING.AsRuntimeClass(ffi.vm->Types))
                ->AsNative((delegate*<CallFrame*, IshtarObject**, IshtarObject*>)&FReadLine);
        }
    }
}
