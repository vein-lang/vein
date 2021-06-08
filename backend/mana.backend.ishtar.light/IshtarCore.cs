namespace mana.backend.ishtar.light
{
    using global::ishtar;
    using runtime;

    public class IshtarCore
    {
        public static void INIT()
        {
            var asmName = "corlib%";
            ManaCore.Types.ObjectType = new ManaTypeImpl($"{asmName}global::mana/lang/Object", ManaTypeCode.TYPE_OBJECT);
            ManaCore.Types.ValueType = new ManaTypeImpl($"{asmName}global::mana/lang/ValueType", ManaTypeCode.TYPE_CLASS);
            ManaCore.Types.VoidType = new ManaTypeImpl($"{asmName}global::mana/lang/Void", ManaTypeCode.TYPE_VOID);
            ManaCore.Types.StringType = new ManaTypeImpl($"{asmName}global::mana/lang/String", ManaTypeCode.TYPE_STRING);
            ManaCore.Types.ByteType = new ManaTypeImpl($"{asmName}global::mana/lang/Byte", ManaTypeCode.TYPE_I1);
            ManaCore.Types.Int16Type = new ManaTypeImpl($"{asmName}global::mana/lang/Int16", ManaTypeCode.TYPE_I2);
            ManaCore.Types.Int32Type = new ManaTypeImpl($"{asmName}global::mana/lang/Int32", ManaTypeCode.TYPE_I4);
            ManaCore.Types.Int64Type = new ManaTypeImpl($"{asmName}global::mana/lang/Int64", ManaTypeCode.TYPE_I8);
            ManaCore.Types.UInt16Type = new ManaTypeImpl($"{asmName}global::mana/lang/UInt16", ManaTypeCode.TYPE_U2);
            ManaCore.Types.UInt32Type = new ManaTypeImpl($"{asmName}global::mana/lang/UInt32", ManaTypeCode.TYPE_U4);
            ManaCore.Types.UInt64Type = new ManaTypeImpl($"{asmName}global::mana/lang/UInt64", ManaTypeCode.TYPE_U8);
            ManaCore.Types.HalfType = new ManaTypeImpl($"{asmName}global::mana/lang/Half", ManaTypeCode.TYPE_R2);
            ManaCore.Types.FloatType = new ManaTypeImpl($"{asmName}global::mana/lang/Float", ManaTypeCode.TYPE_R4);
            ManaCore.Types.DoubleType = new ManaTypeImpl($"{asmName}global::mana/lang/Double", ManaTypeCode.TYPE_R8);
            ManaCore.Types.DecimalType = new ManaTypeImpl($"{asmName}global::mana/lang/Decimal", ManaTypeCode.TYPE_R16);
            ManaCore.Types.CharType = new ManaTypeImpl($"{asmName}global::mana/lang/Char", ManaTypeCode.TYPE_CHAR);
            ManaCore.Types.BoolType = new ManaTypeImpl($"{asmName}global::mana/lang/Boolean", ManaTypeCode.TYPE_BOOLEAN);
            ManaCore.Types.ArrayType = new ManaTypeImpl($"{asmName}global::mana/lang/Array", ManaTypeCode.TYPE_ARRAY);
            ManaCore.Types.ExceptionType = new ManaTypeImpl($"{asmName}global::mana/lang/Exception", ManaTypeCode.TYPE_CLASS);


            ManaCore.ObjectClass = new RuntimeIshtarClass(ManaCore.Types.ObjectType, null);
            ManaCore.ValueTypeClass = new RuntimeIshtarClass(ManaCore.Types.ValueType, ManaCore.ObjectClass);
            ManaCore.VoidClass = new RuntimeIshtarClass(ManaCore.Types.VoidType, ManaCore.ObjectClass);
            ManaCore.StringClass = new RuntimeIshtarClass(ManaCore.Types.StringType, ManaCore.ObjectClass);
            ManaCore.ByteClass = new RuntimeIshtarClass(ManaCore.Types.ByteType, ManaCore.ValueTypeClass);
            ManaCore.Int16Class = new RuntimeIshtarClass(ManaCore.Types.Int16Type, ManaCore.ValueTypeClass);
            ManaCore.Int32Class = new RuntimeIshtarClass(ManaCore.Types.Int32Type, ManaCore.ValueTypeClass);
            ManaCore.Int64Class = new RuntimeIshtarClass(ManaCore.Types.Int64Type, ManaCore.ValueTypeClass);
            ManaCore.UInt16Class = new RuntimeIshtarClass(ManaCore.Types.UInt16Type, ManaCore.ValueTypeClass);
            ManaCore.UInt32Class = new RuntimeIshtarClass(ManaCore.Types.UInt32Type, ManaCore.ValueTypeClass);
            ManaCore.UInt64Class = new RuntimeIshtarClass(ManaCore.Types.UInt64Type, ManaCore.ValueTypeClass);
            ManaCore.HalfClass = new RuntimeIshtarClass(ManaCore.Types.HalfType, ManaCore.ValueTypeClass);
            ManaCore.FloatClass = new RuntimeIshtarClass(ManaCore.Types.FloatType, ManaCore.ValueTypeClass);
            ManaCore.DoubleClass = new RuntimeIshtarClass(ManaCore.Types.DoubleType, ManaCore.ValueTypeClass);
            ManaCore.DecimalClass = new RuntimeIshtarClass(ManaCore.Types.DecimalType, ManaCore.ValueTypeClass);
            ManaCore.BoolClass = new RuntimeIshtarClass(ManaCore.Types.BoolType, ManaCore.ValueTypeClass);
            ManaCore.CharClass = new RuntimeIshtarClass(ManaCore.Types.CharType, ManaCore.ValueTypeClass);
            ManaCore.ArrayClass = new RuntimeIshtarClass(ManaCore.Types.ArrayType, ManaCore.ObjectClass);
            ManaCore.ExceptionClass = new RuntimeIshtarClass(ManaCore.Types.ExceptionType, ManaCore.ObjectClass);




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
        }
    }
}
