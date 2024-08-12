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

            current->vm->trace.console_std_write(str);
            return null;
        }

        [IshtarExport(0, "@_readline")]
        [IshtarExportFlags(Public | Static)]
        public static IshtarObject* FReadLine(CallFrame* current, IshtarObject** args)
            => current->GetGC()->ToIshtarObject(In.ReadLine(), current);


        public static void InitTable(ForeignFunctionInterface ffi)
        {
            ffi.Add("@_println", Public | Static | Extern, TYPE_VOID, ("val", TYPE_OBJECT))
                ->AsNative((delegate*<CallFrame*, IshtarObject**, IshtarObject*>)&FPrintLn_Object);
            ffi.Add("@_readline", Public | Static | Extern,  TYPE_STRING.AsRuntimeClass(ffi.vm->Types))
                ->AsNative((delegate*<CallFrame*, IshtarObject**, IshtarObject*>)&FReadLine);
        }
    }
}
