namespace insomnia.emit
{
    using System.Collections.Generic;

    public static class WaveCore
    {
        public static class Types
        {
            public static List<WaveType> All => new()
            {
                ObjectType,
                ValueType,
                VoidType,
                StringType,
                Int32Type,
                Int16Type,
                Int64Type,
                UInt32Type,
                UInt16Type,
                UInt64Type,
                FloatType,
                DoubleType,
                DecimalType,
                CharType,
                BoolType,
                ArrayType,
                ExceptionType
            };
            public static WaveType ObjectType { get; private set; }
            public static WaveType ValueType { get; private set; }
            public static WaveType VoidType { get; private set; }
            public static WaveType StringType { get; private set; }
            public static WaveType ByteType { get; private set; }
            public static WaveType Int32Type { get; private set; }
            public static WaveType Int16Type { get; private set; }
            public static WaveType Int64Type { get; private set; }
            public static WaveType UInt32Type { get; private set; }
            public static WaveType UInt16Type { get; private set; }
            public static WaveType UInt64Type { get; private set; }
            public static WaveType HalfType { get; private set; }
            public static WaveType FloatType { get; private set; }
            public static WaveType DoubleType { get; private set; }
            public static WaveType DecimalType { get; private set; }
            public static WaveType CharType { get; private set; }
            public static WaveType BoolType { get; private set; }
            public static WaveType ArrayType { get; private set; }
            public static WaveType ExceptionType { get; private set; }

            internal static void Init()
            {
                var asmName = "corlib%";
                ObjectType      = new WaveTypeImpl($"{asmName}global::wave/lang/Object"   , WaveTypeCode.TYPE_OBJECT);
                ValueType       = new WaveTypeImpl($"{asmName}global::wave/lang/ValueType", WaveTypeCode.TYPE_CLASS);
                VoidType        = new WaveTypeImpl($"{asmName}global::wave/lang/Void"     , WaveTypeCode.TYPE_VOID);
                StringType      = new WaveTypeImpl($"{asmName}global::wave/lang/String"   , WaveTypeCode.TYPE_STRING);
                ByteType        = new WaveTypeImpl($"{asmName}global::wave/lang/Byte"    , WaveTypeCode.TYPE_I1);
                Int16Type       = new WaveTypeImpl($"{asmName}global::wave/lang/Int16"    , WaveTypeCode.TYPE_I2);
                Int32Type       = new WaveTypeImpl($"{asmName}global::wave/lang/Int32"    , WaveTypeCode.TYPE_I4);
                Int64Type       = new WaveTypeImpl($"{asmName}global::wave/lang/Int64"    , WaveTypeCode.TYPE_I8);
                UInt16Type      = new WaveTypeImpl($"{asmName}global::wave/lang/UInt16"   , WaveTypeCode.TYPE_U2);
                UInt32Type      = new WaveTypeImpl($"{asmName}global::wave/lang/UInt32"   , WaveTypeCode.TYPE_U4);
                UInt64Type      = new WaveTypeImpl($"{asmName}global::wave/lang/UInt64"   , WaveTypeCode.TYPE_U8);
                HalfType        = new WaveTypeImpl($"{asmName}global::wave/lang/Half"     , WaveTypeCode.TYPE_R2);
                FloatType       = new WaveTypeImpl($"{asmName}global::wave/lang/Float"    , WaveTypeCode.TYPE_R4);
                DoubleType      = new WaveTypeImpl($"{asmName}global::wave/lang/Double"   , WaveTypeCode.TYPE_R8);
                DecimalType     = new WaveTypeImpl($"{asmName}global::wave/lang/Decimal"  , WaveTypeCode.TYPE_R16);
                CharType        = new WaveTypeImpl($"{asmName}global::wave/lang/Char"     , WaveTypeCode.TYPE_CHAR);
                BoolType        = new WaveTypeImpl($"{asmName}global::wave/lang/Boolean"  , WaveTypeCode.TYPE_BOOLEAN);
                ArrayType       = new WaveTypeImpl($"{asmName}global::wave/lang/Array"    , WaveTypeCode.TYPE_ARRAY);
                ExceptionType   = new WaveTypeImpl($"{asmName}global::wave/lang/Exception", WaveTypeCode.TYPE_CLASS);
            }
        }
        public static WaveClass ObjectClass;
        public static WaveClass ValueTypeClass;
        public static WaveClass VoidClass;
        public static WaveClass StringClass;
        public static WaveClass ByteClass;
        public static WaveClass Int32Class;
        public static WaveClass Int16Class;
        public static WaveClass Int64Class;
        public static WaveClass UInt32Class;
        public static WaveClass UInt16Class;
        public static WaveClass UInt64Class;
        public static WaveClass HalfClass;
        public static WaveClass FloatClass;
        public static WaveClass DoubleClass;
        public static WaveClass DecimalClass;
        public static WaveClass BoolClass;
        public static WaveClass CharClass;
        public static WaveClass ArrayClass;
        public static WaveClass ExceptionClass;

        public static List<WaveClass> All => new()
        {
            ObjectClass,
            ValueTypeClass,
            VoidClass,
            StringClass,
            ByteClass,
            Int32Class,
            Int64Class,
            Int16Class,
            UInt32Class,
            UInt64Class,
            UInt16Class,
            HalfClass,
            FloatClass,
            DoubleClass,
            DecimalClass,
            BoolClass,
            CharClass,
            ArrayClass,
            ExceptionClass
        };
        
        public static void Init()
        {
            Types.Init();
            ObjectClass = new WaveClass(Types.ObjectType, null);
            ValueTypeClass = new WaveClass(Types.ValueType, ObjectClass);
            VoidClass = new WaveClass(Types.VoidType, ObjectClass);
            StringClass = new WaveClass(Types.StringType, ObjectClass);
            ByteClass = new WaveClass(Types.ByteType, ValueTypeClass);
            Int16Class = new WaveClass(Types.Int16Type, ValueTypeClass);
            Int32Class = new WaveClass(Types.Int32Type, ValueTypeClass);
            Int64Class = new WaveClass(Types.Int64Type, ValueTypeClass);
            UInt16Class = new WaveClass(Types.UInt16Type, ValueTypeClass);
            UInt32Class = new WaveClass(Types.UInt32Type, ValueTypeClass);
            UInt64Class = new WaveClass(Types.UInt64Type, ValueTypeClass);
            HalfClass = new WaveClass(Types.HalfType, ValueTypeClass);
            FloatClass = new WaveClass(Types.FloatType, ValueTypeClass);
            DoubleClass = new WaveClass(Types.DoubleType, ValueTypeClass);
            DecimalClass = new WaveClass(Types.DecimalType, ValueTypeClass);
            BoolClass = new WaveClass(Types.BoolType, ValueTypeClass);
            CharClass = new WaveClass(Types.CharType, ValueTypeClass);
            ArrayClass = new WaveClass(Types.ArrayType, ObjectClass);
            ExceptionClass = new WaveClass(Types.ExceptionType, ObjectClass);
            
            
            ObjectClass.DefineMethod("getHashCode", Types.Int32Type, 
                MethodFlags.Virtual | MethodFlags.Public);
            ObjectClass.DefineMethod("toString", Types.StringType,
                MethodFlags.Virtual | MethodFlags.Public);
            ArrayClass.DefineMethod("resize", Types.ArrayType,
                MethodFlags.Public | MethodFlags.Extern | MethodFlags.Static,
                ("arr", WaveTypeCode.TYPE_ARRAY), ("newSize", WaveTypeCode.TYPE_I4));
            ExceptionClass.DefineMethod("ctor", WaveTypeCode.TYPE_VOID.AsType(), MethodFlags.Public);
        }
        
        static WaveCore()
        {
            
        }
    }
}