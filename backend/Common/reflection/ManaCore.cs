namespace mana.runtime
{
    using System.Collections.Generic;

    public static class ManaCore
    {
        public static class Types
        {
            public static List<ManaType> All => new()
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
                HalfType,
                CharType,
                BoolType,
                ArrayType,
                ExceptionType
            };
            public static ManaType ObjectType { get; internal set; }
            public static ManaType ValueType { get; internal set; }
            public static ManaType VoidType { get; internal set; }
            public static ManaType StringType { get; internal set; }
            public static ManaType ByteType { get; internal set; }
            public static ManaType Int32Type { get; internal set; }
            public static ManaType Int16Type { get; internal set; }
            public static ManaType Int64Type { get; internal set; }
            public static ManaType UInt32Type { get; internal set; }
            public static ManaType UInt16Type { get; internal set; }
            public static ManaType UInt64Type { get; internal set; }
            public static ManaType HalfType { get; internal set; }
            public static ManaType FloatType { get; internal set; }
            public static ManaType DoubleType { get; internal set; }
            public static ManaType DecimalType { get; internal set; }
            public static ManaType CharType { get; internal set; }
            public static ManaType BoolType { get; internal set; }
            public static ManaType ArrayType { get; internal set; }
            public static ManaType ExceptionType { get; internal set; }

            internal static void Init()
            {
                var asmName = "corlib%";
                ObjectType      = new ManaTypeImpl($"{asmName}global::mana/lang/Object"   , ManaTypeCode.TYPE_OBJECT);
                ValueType       = new ManaTypeImpl($"{asmName}global::mana/lang/ValueType", ManaTypeCode.TYPE_CLASS);
                VoidType        = new ManaTypeImpl($"{asmName}global::mana/lang/Void"     , ManaTypeCode.TYPE_VOID);
                StringType      = new ManaTypeImpl($"{asmName}global::mana/lang/String"   , ManaTypeCode.TYPE_STRING);
                ByteType        = new ManaTypeImpl($"{asmName}global::mana/lang/Byte"     , ManaTypeCode.TYPE_I1);
                Int16Type       = new ManaTypeImpl($"{asmName}global::mana/lang/Int16"    , ManaTypeCode.TYPE_I2);
                Int32Type       = new ManaTypeImpl($"{asmName}global::mana/lang/Int32"    , ManaTypeCode.TYPE_I4);
                Int64Type       = new ManaTypeImpl($"{asmName}global::mana/lang/Int64"    , ManaTypeCode.TYPE_I8);
                UInt16Type      = new ManaTypeImpl($"{asmName}global::mana/lang/UInt16"   , ManaTypeCode.TYPE_U2);
                UInt32Type      = new ManaTypeImpl($"{asmName}global::mana/lang/UInt32"   , ManaTypeCode.TYPE_U4);
                UInt64Type      = new ManaTypeImpl($"{asmName}global::mana/lang/UInt64"   , ManaTypeCode.TYPE_U8);
                HalfType        = new ManaTypeImpl($"{asmName}global::mana/lang/Half"     , ManaTypeCode.TYPE_R2);
                FloatType       = new ManaTypeImpl($"{asmName}global::mana/lang/Float"    , ManaTypeCode.TYPE_R4);
                DoubleType      = new ManaTypeImpl($"{asmName}global::mana/lang/Double"   , ManaTypeCode.TYPE_R8);
                DecimalType     = new ManaTypeImpl($"{asmName}global::mana/lang/Decimal"  , ManaTypeCode.TYPE_R16);
                CharType        = new ManaTypeImpl($"{asmName}global::mana/lang/Char"     , ManaTypeCode.TYPE_CHAR);
                BoolType        = new ManaTypeImpl($"{asmName}global::mana/lang/Boolean"  , ManaTypeCode.TYPE_BOOLEAN);
                ArrayType       = new ManaTypeImpl($"{asmName}global::mana/lang/Array"    , ManaTypeCode.TYPE_ARRAY);
                ExceptionType   = new ManaTypeImpl($"{asmName}global::mana/lang/Exception", ManaTypeCode.TYPE_CLASS);
            }
        }
        public static ManaClass ObjectClass;
        public static ManaClass ValueTypeClass;
        public static ManaClass VoidClass;
        public static ManaClass StringClass;
        public static ManaClass ByteClass;
        public static ManaClass Int32Class;
        public static ManaClass Int16Class;
        public static ManaClass Int64Class;
        public static ManaClass UInt32Class;
        public static ManaClass UInt16Class;
        public static ManaClass UInt64Class;
        public static ManaClass HalfClass;
        public static ManaClass FloatClass;
        public static ManaClass DoubleClass;
        public static ManaClass DecimalClass;
        public static ManaClass BoolClass;
        public static ManaClass CharClass;
        public static ManaClass ArrayClass;
        public static ManaClass ExceptionClass;

        public static List<ManaClass> All => new()
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
            ObjectClass = new ManaClass(Types.ObjectType, null);
            ValueTypeClass = new ManaClass(Types.ValueType, ObjectClass);
            VoidClass = new ManaClass(Types.VoidType, ObjectClass);
            StringClass = new ManaClass(Types.StringType, ObjectClass);
            ByteClass = new ManaClass(Types.ByteType, ValueTypeClass);
            Int16Class = new ManaClass(Types.Int16Type, ValueTypeClass);
            Int32Class = new ManaClass(Types.Int32Type, ValueTypeClass);
            Int64Class = new ManaClass(Types.Int64Type, ValueTypeClass);
            UInt16Class = new ManaClass(Types.UInt16Type, ValueTypeClass);
            UInt32Class = new ManaClass(Types.UInt32Type, ValueTypeClass);
            UInt64Class = new ManaClass(Types.UInt64Type, ValueTypeClass);
            HalfClass = new ManaClass(Types.HalfType, ValueTypeClass);
            FloatClass = new ManaClass(Types.FloatType, ValueTypeClass);
            DoubleClass = new ManaClass(Types.DoubleType, ValueTypeClass);
            DecimalClass = new ManaClass(Types.DecimalType, ValueTypeClass);
            BoolClass = new ManaClass(Types.BoolType, ValueTypeClass);
            CharClass = new ManaClass(Types.CharType, ValueTypeClass);
            ArrayClass = new ManaClass(Types.ArrayType, ObjectClass);
            ExceptionClass = new ManaClass(Types.ExceptionType, ObjectClass);
        }
        
        static ManaCore()
        {
            
        }
    }
}