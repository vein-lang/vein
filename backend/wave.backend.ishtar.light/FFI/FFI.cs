namespace ishtar
{
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using wave.runtime;

    public static unsafe class FFI
    {
        public static Dictionary<string, RuntimeIshtarMethod> method_table = new();

        public static void InitFunctionTable()
        {
            FE_Out.InitTable(method_table);
        }


        [Conditional("STATIC_VALIDATE_IL")]
        public static void StaticValidate(void* p)
        {
            if (p != null) return;
            VM.FastFail(WNE.STATE_CORRUPT, "Null pointer state.");
            VM.ValidateLastError();
        }
        [Conditional("STATIC_VALIDATE_IL")]
        public static void StaticValidateField(CallFrame current, IshtarObject** arg1, string name)
        {
            StaticValidate(*arg1);
            var @class = (*arg1)->DecodeClass();
            VM.Assert(@class.Fields.Any(x => x.Name.Equals(name)), WNE.TYPE_LOAD, 
                $"Field '{name}' not found in '{@class.Name}'.", current);
        }
        [Conditional("STATIC_VALIDATE_IL")]
        public static void StaticValidate(CallFrame current, IshtarObject** arg1)
        {
            StaticValidate(*arg1);
            var @class = (*arg1)->DecodeClass();
            VM.Assert(@class.is_inited, WNE.TYPE_LOAD, $"Class '{@class.FullName}' corrupted.", current);
            VM.Assert(!@class.IsAbstract, WNE.TYPE_LOAD, $"Class '{@class.FullName}' abstract.", current);
        }
        [Conditional("STATIC_VALIDATE_IL")]
        public static void StaticTypeOf(CallFrame current, IshtarObject** arg1, WaveTypeCode code)
        {
            StaticValidate(*arg1);
            var @class = (*arg1)->DecodeClass();
            VM.Assert(@class.TypeCode != code, WNE.MISSING_METHOD, "", current);
        }

        public static RuntimeIshtarMethod GetMethod(string FullName) 
            => method_table.GetValueOrDefault(FullName);
    }
}