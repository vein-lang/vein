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
        public VeinClass ExceptionClass;
        public VeinClass RawClass;
        public VeinClass AspectClass;
        public VeinClass FunctionClass;


        public VeinCore() => init();


        public List<VeinClass> All =>
        [
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
            ExceptionClass,
            RawClass,
            AspectClass,
            FunctionClass
        ];


        // ReSharper disable once MethodTooLong
        private void init()
        {
            var stdModule = ModuleNameSymbol.Std;
            var stdNamespace = NamespaceSymbol.Std;

            var coreModule = new VeinModule(stdModule, new Version(1, 0, 0), this);
            ObjectClass = new(new (new ("Object"), stdNamespace, stdModule), (VeinClass)null, coreModule) { TypeCode = VeinTypeCode.TYPE_OBJECT };
            ValueTypeClass = new(new(new("ValueType"), stdNamespace, stdModule), (VeinClass)null, coreModule) { TypeCode = VeinTypeCode.TYPE_OBJECT };
            VoidClass = new(new(new("Void"), stdNamespace, stdModule), ValueTypeClass, coreModule) { TypeCode = VeinTypeCode.TYPE_VOID };
            StringClass = new(new(new("String"), stdNamespace, stdModule), ObjectClass, coreModule) { TypeCode = VeinTypeCode.TYPE_STRING };
            ByteClass = new(new(new("Byte"), stdNamespace, stdModule), ValueTypeClass, coreModule) { TypeCode = VeinTypeCode.TYPE_U1 };
            SByteClass = new(new(new("SByte"), stdNamespace, stdModule), ValueTypeClass, coreModule) { TypeCode = VeinTypeCode.TYPE_I1 };
            Int16Class = new(new(new("Int16"), stdNamespace, stdModule), ValueTypeClass, coreModule) { TypeCode = VeinTypeCode.TYPE_I2 };
            Int32Class = new(new(new("Int32"), stdNamespace, stdModule), ValueTypeClass, coreModule) { TypeCode = VeinTypeCode.TYPE_I4 };
            Int64Class = new(new(new("Int64"), stdNamespace, stdModule), ValueTypeClass, coreModule) { TypeCode = VeinTypeCode.TYPE_I8 };
            UInt16Class = new(new(new("UInt16"), stdNamespace, stdModule), ValueTypeClass, coreModule) { TypeCode = VeinTypeCode.TYPE_U2 };
            UInt32Class = new(new(new("UInt32"), stdNamespace, stdModule), ValueTypeClass, coreModule) { TypeCode = VeinTypeCode.TYPE_U4 };
            UInt64Class = new(new(new("UInt64"), stdNamespace, stdModule), ValueTypeClass, coreModule) { TypeCode = VeinTypeCode.TYPE_U8 };
            HalfClass = new(new(new("Half"), stdNamespace, stdModule), ValueTypeClass, coreModule) { TypeCode = VeinTypeCode.TYPE_R2 };
            FloatClass = new(new(new("Float"), stdNamespace, stdModule), ValueTypeClass, coreModule) { TypeCode = VeinTypeCode.TYPE_R4 };
            DoubleClass = new(new(new("Double"), stdNamespace, stdModule), ValueTypeClass, coreModule) { TypeCode = VeinTypeCode.TYPE_R8 };
            DecimalClass = new(new(new("Decimal"), stdNamespace, stdModule), ValueTypeClass, coreModule) { TypeCode = VeinTypeCode.TYPE_R16 };
            BoolClass = new(new(new("Boolean"), stdNamespace, stdModule), ValueTypeClass, coreModule) { TypeCode = VeinTypeCode.TYPE_BOOLEAN };
            CharClass = new(new(new("Char"), stdNamespace, stdModule), ValueTypeClass, coreModule) { TypeCode = VeinTypeCode.TYPE_CHAR };
            ExceptionClass = new(new(new("Exception"), stdNamespace, stdModule), ObjectClass, coreModule) { TypeCode = VeinTypeCode.TYPE_CLASS };
            RawClass = new(new(new("Raw"), stdNamespace, stdModule), ObjectClass, coreModule) { TypeCode = VeinTypeCode.TYPE_RAW };
            AspectClass = new(new(new("Aspect"), stdNamespace, stdModule), ObjectClass, coreModule) { TypeCode = VeinTypeCode.TYPE_CLASS };
            FunctionClass = new(new(new("Function"), stdNamespace, stdModule), ObjectClass, coreModule) { TypeCode = VeinTypeCode.TYPE_FUNCTION };
        }
    }
}
