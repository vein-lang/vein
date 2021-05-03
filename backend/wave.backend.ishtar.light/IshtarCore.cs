namespace wave.backend.ishtar.light
{
    using global::ishtar;
    using runtime;

    public class IshtarCore
    {
        public static void Init()
        {
            var asmName = "corlib%";
            WaveCore.Types.ObjectType      = new WaveTypeImpl($"{asmName}global::wave/lang/Object"   , WaveTypeCode.TYPE_OBJECT);
            WaveCore.Types.ValueType       = new WaveTypeImpl($"{asmName}global::wave/lang/ValueType", WaveTypeCode.TYPE_CLASS);
            WaveCore.Types.VoidType        = new WaveTypeImpl($"{asmName}global::wave/lang/Void"     , WaveTypeCode.TYPE_VOID);
            WaveCore.Types.StringType      = new WaveTypeImpl($"{asmName}global::wave/lang/String"   , WaveTypeCode.TYPE_STRING);
            WaveCore.Types.ByteType        = new WaveTypeImpl($"{asmName}global::wave/lang/Byte"     , WaveTypeCode.TYPE_I1);
            WaveCore.Types.Int16Type       = new WaveTypeImpl($"{asmName}global::wave/lang/Int16"    , WaveTypeCode.TYPE_I2);
            WaveCore.Types.Int32Type       = new WaveTypeImpl($"{asmName}global::wave/lang/Int32"    , WaveTypeCode.TYPE_I4);
            WaveCore.Types.Int64Type       = new WaveTypeImpl($"{asmName}global::wave/lang/Int64"    , WaveTypeCode.TYPE_I8);
            WaveCore.Types.UInt16Type      = new WaveTypeImpl($"{asmName}global::wave/lang/UInt16"   , WaveTypeCode.TYPE_U2);
            WaveCore.Types.UInt32Type      = new WaveTypeImpl($"{asmName}global::wave/lang/UInt32"   , WaveTypeCode.TYPE_U4);
            WaveCore.Types.UInt64Type      = new WaveTypeImpl($"{asmName}global::wave/lang/UInt64"   , WaveTypeCode.TYPE_U8);
            WaveCore.Types.HalfType        = new WaveTypeImpl($"{asmName}global::wave/lang/Half"     , WaveTypeCode.TYPE_R2);
            WaveCore.Types.FloatType       = new WaveTypeImpl($"{asmName}global::wave/lang/Float"    , WaveTypeCode.TYPE_R4);
            WaveCore.Types.DoubleType      = new WaveTypeImpl($"{asmName}global::wave/lang/Double"   , WaveTypeCode.TYPE_R8);
            WaveCore.Types.DecimalType     = new WaveTypeImpl($"{asmName}global::wave/lang/Decimal"  , WaveTypeCode.TYPE_R16);
            WaveCore.Types.CharType        = new WaveTypeImpl($"{asmName}global::wave/lang/Char"     , WaveTypeCode.TYPE_CHAR);
            WaveCore.Types.BoolType        = new WaveTypeImpl($"{asmName}global::wave/lang/Boolean"  , WaveTypeCode.TYPE_BOOLEAN);
            WaveCore.Types.ArrayType       = new WaveTypeImpl($"{asmName}global::wave/lang/Array"    , WaveTypeCode.TYPE_ARRAY);
            WaveCore.Types.ExceptionType   = new WaveTypeImpl($"{asmName}global::wave/lang/Exception", WaveTypeCode.TYPE_CLASS);


            WaveCore.ObjectClass    = new RuntimeIshtarClass(WaveCore.Types.ObjectType, null);
            WaveCore.ValueTypeClass = new RuntimeIshtarClass(WaveCore.Types.ValueType   , WaveCore.ObjectClass);
            WaveCore.VoidClass      = new RuntimeIshtarClass(WaveCore.Types.VoidType    , WaveCore.ObjectClass);
            WaveCore.StringClass    = new RuntimeIshtarClass(WaveCore.Types.StringType  , WaveCore.ObjectClass);
            WaveCore.ByteClass      = new RuntimeIshtarClass(WaveCore.Types.ByteType    , WaveCore.ValueTypeClass);
            WaveCore.Int16Class     = new RuntimeIshtarClass(WaveCore.Types.Int16Type   , WaveCore.ValueTypeClass);
            WaveCore.Int32Class     = new RuntimeIshtarClass(WaveCore.Types.Int32Type   , WaveCore.ValueTypeClass);
            WaveCore.Int64Class     = new RuntimeIshtarClass(WaveCore.Types.Int64Type   , WaveCore.ValueTypeClass);
            WaveCore.UInt16Class    = new RuntimeIshtarClass(WaveCore.Types.UInt16Type  , WaveCore.ValueTypeClass);
            WaveCore.UInt32Class    = new RuntimeIshtarClass(WaveCore.Types.UInt32Type  , WaveCore.ValueTypeClass);
            WaveCore.UInt64Class    = new RuntimeIshtarClass(WaveCore.Types.UInt64Type  , WaveCore.ValueTypeClass);
            WaveCore.HalfClass      = new RuntimeIshtarClass(WaveCore.Types.HalfType    , WaveCore.ValueTypeClass);
            WaveCore.FloatClass     = new RuntimeIshtarClass(WaveCore.Types.FloatType   , WaveCore.ValueTypeClass);
            WaveCore.DoubleClass    = new RuntimeIshtarClass(WaveCore.Types.DoubleType  , WaveCore.ValueTypeClass);
            WaveCore.DecimalClass   = new RuntimeIshtarClass(WaveCore.Types.DecimalType , WaveCore.ValueTypeClass);
            WaveCore.BoolClass      = new RuntimeIshtarClass(WaveCore.Types.BoolType    , WaveCore.ValueTypeClass);
            WaveCore.CharClass      = new RuntimeIshtarClass(WaveCore.Types.CharType    , WaveCore.ValueTypeClass);
            WaveCore.ArrayClass     = new RuntimeIshtarClass(WaveCore.Types.ArrayType   , WaveCore.ObjectClass);
            WaveCore.ExceptionClass = new RuntimeIshtarClass(WaveCore.Types.ExceptionType, WaveCore.ObjectClass);




            (WaveCore.ValueTypeClass as RuntimeIshtarClass)
                .DefineField("!!value", FieldFlags.Special,
                WaveCore.ObjectClass);

            (WaveCore.StringClass as RuntimeIshtarClass)
                .DefineField("!!value", FieldFlags.Special,
                    WaveCore.ObjectClass);
        }
    }
}