namespace vein.runtime
{
    using System;
    using System.Collections.Generic;

    public class VeinCore
    {
        public VeinClass ObjectClass;
        public VeinClass ValueTypeClass;
        public VeinClass VoidClass;
        public VeinClass StringClass;
        public VeinClass ByteClass;
        public VeinClass SByteClass;
        public VeinClass Int32Class;
        public VeinClass Int16Class;
        public VeinClass Int64Class;
        public VeinClass UInt32Class;
        public VeinClass UInt16Class;
        public VeinClass UInt64Class;
        public VeinClass HalfClass;
        public VeinClass FloatClass;
        public VeinClass DoubleClass;
        public VeinClass DecimalClass;
        public VeinClass BoolClass;
        public VeinClass CharClass;
        public VeinClass ArrayClass;
        public VeinClass ExceptionClass;
        public VeinClass RawClass;
        public VeinClass AspectClass;
        public VeinClass FunctionClass;


        public VeinCore() => init();


        public List<VeinClass> All => new()
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
            ExceptionClass,
            RawClass,
            AspectClass,
            FunctionClass
        };


        // ReSharper disable once MethodTooLong
        private void init()
        {
            var asmName = "std%";
            var coreModule = new VeinModule("std", new Version(1, 0, 0), this);
            ObjectClass = new($"{asmName}global::vein/lang/Object", (VeinClass)null, coreModule) { TypeCode = VeinTypeCode.TYPE_OBJECT };
            ValueTypeClass = new($"{asmName}global::vein/lang/ValueType", (VeinClass)null, coreModule) { TypeCode = VeinTypeCode.TYPE_OBJECT };
            VoidClass = new($"{asmName}global::vein/lang/Void", ValueTypeClass, coreModule) { TypeCode = VeinTypeCode.TYPE_VOID };
            StringClass = new($"{asmName}global::vein/lang/String", ObjectClass, coreModule) { TypeCode = VeinTypeCode.TYPE_STRING };
            ByteClass = new($"{asmName}global::vein/lang/Byte", ValueTypeClass, coreModule) { TypeCode = VeinTypeCode.TYPE_U1 };
            SByteClass = new($"{asmName}global::vein/lang/SByte", ValueTypeClass, coreModule) { TypeCode = VeinTypeCode.TYPE_I1 };
            Int16Class = new($"{asmName}global::vein/lang/Int16", ValueTypeClass, coreModule) { TypeCode = VeinTypeCode.TYPE_I2 };
            Int32Class = new($"{asmName}global::vein/lang/Int32", ValueTypeClass, coreModule) { TypeCode = VeinTypeCode.TYPE_I4 };
            Int64Class = new($"{asmName}global::vein/lang/Int64", ValueTypeClass, coreModule) { TypeCode = VeinTypeCode.TYPE_I8 };
            UInt16Class = new($"{asmName}global::vein/lang/UInt16", ValueTypeClass, coreModule) { TypeCode = VeinTypeCode.TYPE_U2 };
            UInt32Class = new($"{asmName}global::vein/lang/UInt32", ValueTypeClass, coreModule) { TypeCode = VeinTypeCode.TYPE_U4 };
            UInt64Class = new($"{asmName}global::vein/lang/UInt64", ValueTypeClass, coreModule) { TypeCode = VeinTypeCode.TYPE_U8 };
            HalfClass = new($"{asmName}global::vein/lang/Half", ValueTypeClass, coreModule) { TypeCode = VeinTypeCode.TYPE_R2 };
            FloatClass = new($"{asmName}global::vein/lang/Float", ValueTypeClass, coreModule) { TypeCode = VeinTypeCode.TYPE_R4 };
            DoubleClass = new($"{asmName}global::vein/lang/Double", ValueTypeClass, coreModule) { TypeCode = VeinTypeCode.TYPE_R8 };
            DecimalClass = new($"{asmName}global::vein/lang/Decimal", ValueTypeClass, coreModule) { TypeCode = VeinTypeCode.TYPE_R16 };
            BoolClass = new($"{asmName}global::vein/lang/Boolean", ValueTypeClass, coreModule) { TypeCode = VeinTypeCode.TYPE_BOOLEAN };
            CharClass = new($"{asmName}global::vein/lang/Char", ValueTypeClass, coreModule) { TypeCode = VeinTypeCode.TYPE_CHAR };
            ArrayClass = new($"{asmName}global::vein/lang/Array", ObjectClass, coreModule) { TypeCode = VeinTypeCode.TYPE_ARRAY };
            ExceptionClass = new($"{asmName}global::vein/lang/Exception", ObjectClass, coreModule) { TypeCode = VeinTypeCode.TYPE_CLASS };
            RawClass = new($"{asmName}global::vein/lang/Raw", ObjectClass, coreModule) { TypeCode = VeinTypeCode.TYPE_RAW };
            AspectClass = new($"{asmName}global::vein/lang/Aspect", ObjectClass, coreModule) { TypeCode = VeinTypeCode.TYPE_CLASS };
            FunctionClass = new($"{asmName}global::vein/lang/Function", ObjectClass, coreModule) { TypeCode = VeinTypeCode.TYPE_FUNCTION };
        }
    }
}
