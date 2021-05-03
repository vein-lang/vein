namespace ishtar
{
    using System.Collections.Generic;
    using wave.runtime;

    public static unsafe class FFI
    {
        public static Dictionary<string, RuntimeIshtarMethod> method_table = new();

        public static void InitFunctionTable()
        {
            FE_Out.InitTable(method_table);
        }



        public static void StaticValidate(void* p)
        {
            if (p == null)
            {
                VM.FastFail(WNE.STATE_CORRUPT, "Null pointer state.");
                VM.ValidateLastError();
                return;
            }
        }

        public static void StaticValidate(CallFrame current, IshtarObject** arg1)
        {
            var @class = (*arg1)->Unpack();
            VM.Assert(@class.is_inited, WNE.TYPE_LOAD, $"Class '{@class.FullName}' corrupted.", current);
            VM.Assert(!@class.IsAbstract, WNE.TYPE_LOAD, $"Class '{@class.FullName}' abstract.", current);
        }

        public static void StaticTypeOf(CallFrame current, IshtarObject** arg1, WaveTypeCode code)
        {
            var @class = (*arg1)->Unpack();
            VM.Assert(@class.TypeCode != code, WNE.MISSING_METHOD, "", current);
        }

        public static RuntimeIshtarMethod GetMethod(string FullName) 
            => method_table.GetValueOrDefault(FullName);
    }
}