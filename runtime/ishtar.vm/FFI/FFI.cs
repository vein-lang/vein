namespace ishtar
{
    using System.Collections.Generic;
    using System.Diagnostics;
    using vein.runtime;

    public static unsafe class FFI
    {
        public static Dictionary<string, RuntimeIshtarMethod> method_table { get; } = new();

        public static void INIT()
        {
            B_Out.InitTable(method_table);
            B_App.InitTable(method_table);
            B_IEEEConsts.InitTable(method_table);
            B_Sys.InitTable(method_table);
            B_String.InitTable(method_table);
            B_StringBuilder.InitTable(method_table);
            B_GC.InitTable(method_table);
            X_Utils.InitTable(method_table);
            B_Type.InitTable(method_table);
        }


        [Conditional("STATIC_VALIDATE_IL")]
        public static void StaticValidate(void* p, CallFrame frame)
        {
            if (p != null) return;
            VM.FastFail(WNE.STATE_CORRUPT, "Null pointer state.", frame);
        }
        [Conditional("STATIC_VALIDATE_IL")]
        public static void StaticValidateField(CallFrame current, IshtarObject** arg1, string name)
        {
            StaticValidate(*arg1, current);
            var @class = (*arg1)->decodeClass();
            VM.Assert(@class.FindField(name) != null, WNE.TYPE_LOAD,
                $"Field '{name}' not found in '{@class.Name}'.", current);
        }

        [Conditional("STATIC_VALIDATE_IL")]
        public static void StaticValidate(CallFrame frame, stackval* value, VeinClass clazz)
        {
            frame.assert(clazz is RuntimeIshtarClass);
            frame.assert(value->type != VeinTypeCode.TYPE_NONE);
            var obj = IshtarMarshal.Boxing(frame, value);
            frame.assert(obj->__gc_id != -1);
            var currentClass = obj->decodeClass();
            var targetClass = clazz as RuntimeIshtarClass;
            frame.assert(currentClass.ID == targetClass.ID, $"{currentClass.Name}.ID == {targetClass.Name}.ID");
        }

        [Conditional("STATIC_VALIDATE_IL")]
        public static void StaticValidate(CallFrame current, IshtarObject** arg1)
        {
            StaticValidate(*arg1, current);
            var @class = (*arg1)->decodeClass();
            VM.Assert(@class.is_inited, WNE.TYPE_LOAD, $"Class '{@class.FullName}' corrupted.", current);
            VM.Assert(!@class.IsAbstract, WNE.TYPE_LOAD, $"Class '{@class.FullName}' abstract.", current);
        }
        [Conditional("STATIC_VALIDATE_IL")]
        public static void StaticTypeOf(CallFrame current, IshtarObject** arg1, VeinTypeCode code)
        {
            StaticValidate(*arg1, current);
            var @class = (*arg1)->decodeClass();
            VM.Assert(@class.TypeCode == code, WNE.TYPE_MISMATCH, $"@class.{@class.TypeCode} == {code}", current);
        }

        public static RuntimeIshtarMethod GetMethod(string FullName)
            => method_table.GetValueOrDefault(FullName);
    }
}
