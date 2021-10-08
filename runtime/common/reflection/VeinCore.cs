namespace vein.runtime
{
    using System;
    using System.Collections.Generic;

    public static class VeinCore
    {
        public static VeinClass ObjectClass;
        public static VeinClass ValueTypeClass;
        public static VeinClass VoidClass;
        public static VeinClass StringClass;
        public static VeinClass ByteClass;
        public static VeinClass SByteClass;
        public static VeinClass Int32Class;
        public static VeinClass Int16Class;
        public static VeinClass Int64Class;
        public static VeinClass UInt32Class;
        public static VeinClass UInt16Class;
        public static VeinClass UInt64Class;
        public static VeinClass HalfClass;
        public static VeinClass FloatClass;
        public static VeinClass DoubleClass;
        public static VeinClass DecimalClass;
        public static VeinClass BoolClass;
        public static VeinClass CharClass;
        public static VeinClass ArrayClass;
        public static VeinClass ExceptionClass;

        public static List<VeinClass> All => new()
        {
            ObjectClass,
            ValueTypeClass,
            VoidClass,
            StringClass,
            ByteClass,
            SByteClass,
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
            var asmName = "corlib%";
            var cormodule = new ManaModule("corlib", new Version(1, 0, 0));
            ObjectClass = new VeinClass($"{asmName}global::vein/lang/Object", (VeinClass)null, cormodule) { TypeCode = VeinTypeCode.TYPE_OBJECT };
            ValueTypeClass = new VeinClass($"{asmName}global::vein/lang/ValueType", (VeinClass)null, cormodule) { TypeCode = VeinTypeCode.TYPE_OBJECT };
            VoidClass = new VeinClass($"{asmName}global::vein/lang/Void", ValueTypeClass, cormodule) { TypeCode = VeinTypeCode.TYPE_VOID };
            StringClass = new VeinClass($"{asmName}global::vein/lang/String", ObjectClass, cormodule) { TypeCode = VeinTypeCode.TYPE_STRING };
            ByteClass = new VeinClass($"{asmName}global::vein/lang/Byte", ValueTypeClass, cormodule) { TypeCode = VeinTypeCode.TYPE_U1 };
            SByteClass = new VeinClass($"{asmName}global::vein/lang/SByte", ValueTypeClass, cormodule) { TypeCode = VeinTypeCode.TYPE_I1 };
            Int16Class = new VeinClass($"{asmName}global::vein/lang/Int16", ValueTypeClass, cormodule) { TypeCode = VeinTypeCode.TYPE_I2 };
            Int32Class = new VeinClass($"{asmName}global::vein/lang/Int32", ValueTypeClass, cormodule) { TypeCode = VeinTypeCode.TYPE_I4 };
            Int64Class = new VeinClass($"{asmName}global::vein/lang/Int64", ValueTypeClass, cormodule) { TypeCode = VeinTypeCode.TYPE_I8 };
            UInt16Class = new VeinClass($"{asmName}global::vein/lang/UInt16", ValueTypeClass, cormodule) { TypeCode = VeinTypeCode.TYPE_U2 };
            UInt32Class = new VeinClass($"{asmName}global::vein/lang/UInt32", ValueTypeClass, cormodule) { TypeCode = VeinTypeCode.TYPE_U4 };
            UInt64Class = new VeinClass($"{asmName}global::vein/lang/UInt64", ValueTypeClass, cormodule) { TypeCode = VeinTypeCode.TYPE_U8 };
            HalfClass = new VeinClass($"{asmName}global::vein/lang/Half", ValueTypeClass, cormodule) { TypeCode = VeinTypeCode.TYPE_R2 };
            FloatClass = new VeinClass($"{asmName}global::vein/lang/Float", ValueTypeClass, cormodule) { TypeCode = VeinTypeCode.TYPE_R4 };
            DoubleClass = new VeinClass($"{asmName}global::vein/lang/Double", ValueTypeClass, cormodule) { TypeCode = VeinTypeCode.TYPE_R8 };
            DecimalClass = new VeinClass($"{asmName}global::vein/lang/Decimal", ValueTypeClass, cormodule) { TypeCode = VeinTypeCode.TYPE_R16 };
            BoolClass = new VeinClass($"{asmName}global::vein/lang/Boolean", ValueTypeClass, cormodule) { TypeCode = VeinTypeCode.TYPE_BOOLEAN };
            CharClass = new VeinClass($"{asmName}global::vein/lang/Char", ValueTypeClass, cormodule) { TypeCode = VeinTypeCode.TYPE_CHAR };
            ArrayClass = new VeinClass($"{asmName}global::vein/lang/Array", ObjectClass, cormodule) { TypeCode = VeinTypeCode.TYPE_ARRAY };
            ExceptionClass = new VeinClass($"{asmName}global::vein/lang/Exception", ObjectClass, cormodule) { TypeCode = VeinTypeCode.TYPE_CLASS };
        }
    }
}
