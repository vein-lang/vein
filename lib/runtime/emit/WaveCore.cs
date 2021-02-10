namespace wave.emit
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
                FloatType,
                DoubleType,
                DecimalType,
                CharType,
                BoolType,
                ArrayType
            };
            public static WaveType ObjectType { get; private set; }
            public static WaveType ValueType { get; private set; }
            public static WaveType VoidType { get; private set; }
            public static WaveType StringType { get; private set; }
            public static WaveType Int32Type { get; private set; }
            public static WaveType Int16Type { get; private set; }
            public static WaveType Int64Type { get; private set; }
            public static WaveType FloatType { get; private set; }
            public static WaveType DoubleType { get; private set; }
            public static WaveType DecimalType { get; private set; }
            public static WaveType CharType { get; private set; }
            public static WaveType BoolType { get; private set; }
            public static WaveType ArrayType { get; private set; }

            internal static void Init()
            {
                ObjectType  = new WaveTypeImpl("global::wave/lang/Object"   , WaveTypeCode.TYPE_OBJECT);
                ValueType   = new WaveTypeImpl("global::wave/lang/ValueType", WaveTypeCode.TYPE_CLASS);
                VoidType    = new WaveTypeImpl("global::wave/lang/Void"     , WaveTypeCode.TYPE_VOID);
                StringType  = new WaveTypeImpl("global::wave/lang/String"   , WaveTypeCode.TYPE_STRING);
                Int16Type   = new WaveTypeImpl("global::wave/lang/Int16"    , WaveTypeCode.TYPE_I2);
                Int32Type   = new WaveTypeImpl("global::wave/lang/Int32"    , WaveTypeCode.TYPE_I4);
                Int64Type   = new WaveTypeImpl("global::wave/lang/Int64"    , WaveTypeCode.TYPE_I8);
                FloatType   = new WaveTypeImpl("global::wave/lang/Float"    , WaveTypeCode.TYPE_R4);
                DoubleType  = new WaveTypeImpl("global::wave/lang/Double"   , WaveTypeCode.TYPE_R8);
                DecimalType = new WaveTypeImpl("global::wave/lang/Decimal"  , WaveTypeCode.TYPE_R16);
                CharType    = new WaveTypeImpl("global::wave/lang/Char"     , WaveTypeCode.TYPE_CHAR);
                BoolType    = new WaveTypeImpl("global::wave/lang/Boolean"  , WaveTypeCode.TYPE_BOOLEAN);
                ArrayType   = new WaveTypeImpl("global::wave/lang/Array"    , WaveTypeCode.TYPE_ARRAY);
            }
        }
        public static WaveClass ObjectClass;
        public static WaveClass ValueTypeClass;
        public static WaveClass VoidClass;
        public static WaveClass StringClass;
        public static WaveClass Int32Class;
        public static WaveClass Int16Class;
        public static WaveClass Int64Class;
        public static WaveClass BoolClass;
        public static WaveClass CharClass;
        public static WaveClass ArrayClass;

        public static List<WaveClass> All => new()
        {
            ObjectClass,
            ValueTypeClass,
            VoidClass,
            StringClass,
            Int32Class,
            Int64Class,
            Int16Class,
            BoolClass,
            CharClass,
            ArrayClass
        };
        
        public static void Init()
        {
            Types.Init();
            ObjectClass = new WaveClass(Types.ObjectType, null);
            ValueTypeClass = new WaveClass(Types.ValueType, ObjectClass);
            VoidClass = new WaveClass(Types.VoidType, ObjectClass);
            StringClass = new WaveClass(Types.StringType, ObjectClass);
            Int16Class = new WaveClass(Types.Int16Type, ValueTypeClass);
            Int32Class = new WaveClass(Types.Int32Type, ValueTypeClass);
            Int64Class = new WaveClass(Types.Int64Type, ValueTypeClass);
            BoolClass = new WaveClass(Types.BoolType, ValueTypeClass);
            CharClass = new WaveClass(Types.CharType, ValueTypeClass);
            ArrayClass = new WaveClass(Types.ArrayType, ObjectClass);
            
            
            ObjectClass.DefineMethod("getHashCode", Types.Int32Type, 
                MethodFlags.Virtual | MethodFlags.Public);
            ObjectClass.DefineMethod("toString", Types.StringType,
                MethodFlags.Virtual | MethodFlags.Public);
            ArrayClass.DefineMethod("resize", Types.ArrayType,
                MethodFlags.Public | MethodFlags.Extern | MethodFlags.Static,
                ("arr", WaveTypeCode.TYPE_ARRAY), ("newSize", WaveTypeCode.TYPE_I4));
        }
        
        static WaveCore()
        {
            
        }
    }
}