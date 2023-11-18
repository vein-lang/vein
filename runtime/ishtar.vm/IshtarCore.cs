namespace vein.runtime
{
    using System;
    using ishtar;

    public class IshtarCore : VeinCore
    {
        private readonly VirtualMachine _vm;

        public IshtarCore(VirtualMachine vm)
        {
            _vm = vm;
            Init();
        }


        private void Init()
        {
            var asmName = "std%";

            ObjectClass = new RuntimeIshtarClass($"{asmName}global::vein/lang/Object", (VeinClass)null, null) { TypeCode = VeinTypeCode.TYPE_OBJECT };
            ValueTypeClass = new RuntimeIshtarClass($"{asmName}global::vein/lang/ValueType", (VeinClass)null, null) { TypeCode = VeinTypeCode.TYPE_OBJECT };
            VoidClass = new RuntimeIshtarClass($"{asmName}global::vein/lang/Void", ObjectClass, null) { TypeCode = VeinTypeCode.TYPE_VOID };
            StringClass = new RuntimeIshtarClass($"{asmName}global::vein/lang/String", ObjectClass, null) { TypeCode = VeinTypeCode.TYPE_STRING };
            ByteClass = new RuntimeIshtarClass($"{asmName}global::vein/lang/Byte", ValueTypeClass, null) { TypeCode = VeinTypeCode.TYPE_U1 };
            SByteClass = new RuntimeIshtarClass($"{asmName}global::vein/lang/SByte", ValueTypeClass, null) { TypeCode = VeinTypeCode.TYPE_I1 };
            Int16Class = new RuntimeIshtarClass($"{asmName}global::vein/lang/Int16", ValueTypeClass, null) { TypeCode = VeinTypeCode.TYPE_I2 };
            Int32Class = new RuntimeIshtarClass($"{asmName}global::vein/lang/Int32", ValueTypeClass, null) { TypeCode = VeinTypeCode.TYPE_I4 };
            Int64Class = new RuntimeIshtarClass($"{asmName}global::vein/lang/Int64", ValueTypeClass, null) { TypeCode = VeinTypeCode.TYPE_I8 };
            UInt16Class = new RuntimeIshtarClass($"{asmName}global::vein/lang/UIn16", ValueTypeClass, null) { TypeCode = VeinTypeCode.TYPE_U2 };
            UInt32Class = new RuntimeIshtarClass($"{asmName}global::vein/lang/UInt32", ValueTypeClass, null) { TypeCode = VeinTypeCode.TYPE_U4 };
            UInt64Class = new RuntimeIshtarClass($"{asmName}global::vein/lang/UInt64", ValueTypeClass, null) { TypeCode = VeinTypeCode.TYPE_U8 };
            HalfClass = new RuntimeIshtarClass($"{asmName}global::vein/lang/Half", ValueTypeClass, null) { TypeCode = VeinTypeCode.TYPE_R2 };
            FloatClass = new RuntimeIshtarClass($"{asmName}global::vein/lang/Float", ValueTypeClass, null) { TypeCode = VeinTypeCode.TYPE_R4 };
            DoubleClass = new RuntimeIshtarClass($"{asmName}global::vein/lang/Double", ValueTypeClass, null) { TypeCode = VeinTypeCode.TYPE_R8 };
            DecimalClass = new RuntimeIshtarClass($"{asmName}global::vein/lang/Decimal", ValueTypeClass, null) { TypeCode = VeinTypeCode.TYPE_R16 };
            BoolClass = new RuntimeIshtarClass($"{asmName}global::vein/lang/Boolean", ValueTypeClass, null) { TypeCode = VeinTypeCode.TYPE_BOOLEAN };
            CharClass = new RuntimeIshtarClass($"{asmName}global::vein/lang/Char", ValueTypeClass, null) { TypeCode = VeinTypeCode.TYPE_CHAR };
            ArrayClass = new RuntimeIshtarClass($"{asmName}global::vein/lang/Array", ObjectClass, null) { TypeCode = VeinTypeCode.TYPE_ARRAY };
            ExceptionClass = new RuntimeIshtarClass($"{asmName}global::vein/lang/Exception", ObjectClass, null) { TypeCode = VeinTypeCode.TYPE_OBJECT };
            RawClass = new RuntimeIshtarClass($"{asmName}global::vein/lang/Raw", (VeinClass)null, null) { TypeCode = VeinTypeCode.TYPE_RAW };
            AspectClass = new RuntimeIshtarClass($"{asmName}global::vein/lang/Aspect", ObjectClass, null) { TypeCode = VeinTypeCode.TYPE_CLASS };
            FunctionClass = new RuntimeIshtarClass($"{asmName}global::vein/lang/Function", ObjectClass, null) { TypeCode = VeinTypeCode.TYPE_FUNCTION };

            INIT_ADDITIONAL_MAPPING();
        }

        public void InitVtables()
        {
            foreach (var @class in All.OfType<RuntimeIshtarClass>())
                @class.init_vtable(_vm);
        }


        /// <exception cref="InvalidSystemMappingException">Incorrect initialization step</exception>
        private void INIT_ADDITIONAL_MAPPING()
        {
            if (ValueTypeClass is not RuntimeIshtarClass)
                throw new InvalidSystemMappingException();
            if (StringClass is not RuntimeIshtarClass)
                throw new InvalidSystemMappingException();
            if (ArrayClass is not RuntimeIshtarClass)
                throw new InvalidSystemMappingException();

            (ValueTypeClass as RuntimeIshtarClass)
                !.DefineField("!!value", FieldFlags.Special | FieldFlags.Internal,
                    RawClass);
            (StringClass as RuntimeIshtarClass)
                !.DefineField("!!value", FieldFlags.Special | FieldFlags.Internal,
                    RawClass);

            (ArrayClass as RuntimeIshtarClass)
                !.DefineField("!!value", FieldFlags.Special,
                    RawClass);
            (ArrayClass as RuntimeIshtarClass)
                !.DefineField("!!block", FieldFlags.Special,
                    Int64Class);
            (ArrayClass as RuntimeIshtarClass)
                !.DefineField("!!size", FieldFlags.Special,
                    Int64Class);
            (ArrayClass as RuntimeIshtarClass)
                !.DefineField("!!rank", FieldFlags.Special,
                    Int64Class);
        }
    }

    public class InvalidSystemMappingException : Exception
    {
        public InvalidSystemMappingException() : base($"Incorrect initialization step.\n" +
                    $"Please report the problem into 'https://github.com/vein-lang/vein/issues'. ")
        { }
    }
}
