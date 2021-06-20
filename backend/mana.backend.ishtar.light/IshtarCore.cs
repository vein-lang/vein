namespace mana.backend.ishtar.light
{
    using System;
    using global::ishtar;
    using runtime;

    public class IshtarCore
    {
        public static void INIT()
        {
            var asmName = "corlib%";

            ManaCore.ObjectClass = new RuntimeIshtarClass($"{asmName}global::mana/lang/Object", null, null) { TypeCode = ManaTypeCode.TYPE_OBJECT };
            ManaCore.ValueTypeClass = new RuntimeIshtarClass($"{asmName}global::mana/lang/ValueType", ManaCore.ObjectClass, null) { TypeCode = ManaTypeCode.TYPE_OBJECT };
            ManaCore.VoidClass = new RuntimeIshtarClass($"{asmName}global::mana/lang/Void", ManaCore.ObjectClass, null) { TypeCode = ManaTypeCode.TYPE_VOID };
            ManaCore.StringClass = new RuntimeIshtarClass($"{asmName}global::mana/lang/String", ManaCore.ObjectClass, null) { TypeCode = ManaTypeCode.TYPE_STRING };
            ManaCore.ByteClass = new RuntimeIshtarClass($"{asmName}global::mana/lang/Byte", ManaCore.ValueTypeClass, null) { TypeCode = ManaTypeCode.TYPE_U1 };
            ManaCore.SByteClass = new RuntimeIshtarClass($"{asmName}global::mana/lang/SByte", ManaCore.ValueTypeClass, null) { TypeCode = ManaTypeCode.TYPE_I1 };
            ManaCore.Int16Class = new RuntimeIshtarClass($"{asmName}global::mana/lang/Int16", ManaCore.ValueTypeClass, null) { TypeCode = ManaTypeCode.TYPE_I2 };
            ManaCore.Int32Class = new RuntimeIshtarClass($"{asmName}global::mana/lang/Int32", ManaCore.ValueTypeClass, null) { TypeCode = ManaTypeCode.TYPE_I4 };
            ManaCore.Int64Class = new RuntimeIshtarClass($"{asmName}global::mana/lang/Int64", ManaCore.ValueTypeClass, null) { TypeCode = ManaTypeCode.TYPE_I8 };
            ManaCore.UInt16Class = new RuntimeIshtarClass($"{asmName}global::mana/lang/UIn16", ManaCore.ValueTypeClass, null) { TypeCode = ManaTypeCode.TYPE_U2 };
            ManaCore.UInt32Class = new RuntimeIshtarClass($"{asmName}global::mana/lang/UInt32", ManaCore.ValueTypeClass, null) { TypeCode = ManaTypeCode.TYPE_U4 };
            ManaCore.UInt64Class = new RuntimeIshtarClass($"{asmName}global::mana/lang/UInt64", ManaCore.ValueTypeClass, null) { TypeCode = ManaTypeCode.TYPE_U8 };
            ManaCore.HalfClass = new RuntimeIshtarClass($"{asmName}global::mana/lang/Half", ManaCore.ValueTypeClass, null) { TypeCode = ManaTypeCode.TYPE_R2 };
            ManaCore.FloatClass = new RuntimeIshtarClass($"{asmName}global::mana/lang/Float", ManaCore.ValueTypeClass, null) { TypeCode = ManaTypeCode.TYPE_R4 };
            ManaCore.DoubleClass = new RuntimeIshtarClass($"{asmName}global::mana/lang/Double", ManaCore.ValueTypeClass, null) { TypeCode = ManaTypeCode.TYPE_R8 };
            ManaCore.DecimalClass = new RuntimeIshtarClass($"{asmName}global::mana/lang/Decimal", ManaCore.ValueTypeClass, null) { TypeCode = ManaTypeCode.TYPE_R16 };
            ManaCore.BoolClass = new RuntimeIshtarClass($"{asmName}global::mana/lang/Boolean", ManaCore.ValueTypeClass, null) { TypeCode = ManaTypeCode.TYPE_BOOLEAN };
            ManaCore.CharClass = new RuntimeIshtarClass($"{asmName}global::mana/lang/Char", ManaCore.ValueTypeClass, null) { TypeCode = ManaTypeCode.TYPE_CHAR };
            ManaCore.ArrayClass = new RuntimeIshtarClass($"{asmName}global::mana/lang/Array", ManaCore.ObjectClass, null) { TypeCode = ManaTypeCode.TYPE_ARRAY };
            ManaCore.ExceptionClass = new RuntimeIshtarClass($"{asmName}global::mana/lang/Exception", ManaCore.ObjectClass, null) { TypeCode = ManaTypeCode.TYPE_OBJECT };

            INIT_ADDITIONAL_MAPPING();
        }


        public static void INIT_ADDITIONAL_MAPPING()
        {
            (ManaCore.ValueTypeClass as RuntimeIshtarClass)
                !.DefineField("!!value", FieldFlags.Special | FieldFlags.Internal,
                    ManaCore.ValueTypeClass);
            (ManaCore.StringClass as RuntimeIshtarClass)
                !.DefineField("!!value", FieldFlags.Special | FieldFlags.Internal,
                    ManaCore.ValueTypeClass);

            (ManaCore.ArrayClass as RuntimeIshtarClass)
                !.DefineField("!!value", FieldFlags.Special,
                    ManaCore.ValueTypeClass);
            (ManaCore.ArrayClass as RuntimeIshtarClass)
                !.DefineField("!!block", FieldFlags.Special,
                    ManaCore.Int64Class);
            (ManaCore.ArrayClass as RuntimeIshtarClass)
                !.DefineField("!!size", FieldFlags.Special,
                    ManaCore.Int64Class);
            (ManaCore.ArrayClass as RuntimeIshtarClass)
                !.DefineField("!!rank", FieldFlags.Special,
                    ManaCore.Int64Class);
        }
    }
}
