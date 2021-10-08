namespace vein.runtime
{
    using System;
    using global::ishtar;

    public class IshtarCore
    {
        public static void INIT()
        {
            var asmName = "corlib%";

            VeinCore.ObjectClass = new RuntimeIshtarClass($"{asmName}global::vein/lang/Object", (VeinClass)null, null) { TypeCode = VeinTypeCode.TYPE_OBJECT };
            VeinCore.ValueTypeClass = new RuntimeIshtarClass($"{asmName}global::vein/lang/ValueType", (VeinClass)null, null) { TypeCode = VeinTypeCode.TYPE_OBJECT };
            VeinCore.VoidClass = new RuntimeIshtarClass($"{asmName}global::vein/lang/Void", VeinCore.ObjectClass, null) { TypeCode = VeinTypeCode.TYPE_VOID };
            VeinCore.StringClass = new RuntimeIshtarClass($"{asmName}global::vein/lang/String", VeinCore.ObjectClass, null) { TypeCode = VeinTypeCode.TYPE_STRING };
            VeinCore.ByteClass = new RuntimeIshtarClass($"{asmName}global::vein/lang/Byte", VeinCore.ValueTypeClass, null) { TypeCode = VeinTypeCode.TYPE_U1 };
            VeinCore.SByteClass = new RuntimeIshtarClass($"{asmName}global::vein/lang/SByte", VeinCore.ValueTypeClass, null) { TypeCode = VeinTypeCode.TYPE_I1 };
            VeinCore.Int16Class = new RuntimeIshtarClass($"{asmName}global::vein/lang/Int16", VeinCore.ValueTypeClass, null) { TypeCode = VeinTypeCode.TYPE_I2 };
            VeinCore.Int32Class = new RuntimeIshtarClass($"{asmName}global::vein/lang/Int32", VeinCore.ValueTypeClass, null) { TypeCode = VeinTypeCode.TYPE_I4 };
            VeinCore.Int64Class = new RuntimeIshtarClass($"{asmName}global::vein/lang/Int64", VeinCore.ValueTypeClass, null) { TypeCode = VeinTypeCode.TYPE_I8 };
            VeinCore.UInt16Class = new RuntimeIshtarClass($"{asmName}global::vein/lang/UIn16", VeinCore.ValueTypeClass, null) { TypeCode = VeinTypeCode.TYPE_U2 };
            VeinCore.UInt32Class = new RuntimeIshtarClass($"{asmName}global::vein/lang/UInt32", VeinCore.ValueTypeClass, null) { TypeCode = VeinTypeCode.TYPE_U4 };
            VeinCore.UInt64Class = new RuntimeIshtarClass($"{asmName}global::vein/lang/UInt64", VeinCore.ValueTypeClass, null) { TypeCode = VeinTypeCode.TYPE_U8 };
            VeinCore.HalfClass = new RuntimeIshtarClass($"{asmName}global::vein/lang/Half", VeinCore.ValueTypeClass, null) { TypeCode = VeinTypeCode.TYPE_R2 };
            VeinCore.FloatClass = new RuntimeIshtarClass($"{asmName}global::vein/lang/Float", VeinCore.ValueTypeClass, null) { TypeCode = VeinTypeCode.TYPE_R4 };
            VeinCore.DoubleClass = new RuntimeIshtarClass($"{asmName}global::vein/lang/Double", VeinCore.ValueTypeClass, null) { TypeCode = VeinTypeCode.TYPE_R8 };
            VeinCore.DecimalClass = new RuntimeIshtarClass($"{asmName}global::vein/lang/Decimal", VeinCore.ValueTypeClass, null) { TypeCode = VeinTypeCode.TYPE_R16 };
            VeinCore.BoolClass = new RuntimeIshtarClass($"{asmName}global::vein/lang/Boolean", VeinCore.ValueTypeClass, null) { TypeCode = VeinTypeCode.TYPE_BOOLEAN };
            VeinCore.CharClass = new RuntimeIshtarClass($"{asmName}global::vein/lang/Char", VeinCore.ValueTypeClass, null) { TypeCode = VeinTypeCode.TYPE_CHAR };
            VeinCore.ArrayClass = new RuntimeIshtarClass($"{asmName}global::vein/lang/Array", VeinCore.ObjectClass, null) { TypeCode = VeinTypeCode.TYPE_ARRAY };
            VeinCore.ExceptionClass = new RuntimeIshtarClass($"{asmName}global::vein/lang/Exception", VeinCore.ObjectClass, null) { TypeCode = VeinTypeCode.TYPE_OBJECT };

            INIT_ADDITIONAL_MAPPING();
        }


        public static void INIT_ADDITIONAL_MAPPING()
        {
            if (VeinCore.ValueTypeClass is not RuntimeIshtarClass)
                throw new InvalidSystemMappingException();

            (VeinCore.ValueTypeClass as RuntimeIshtarClass)
                !.DefineField("!!value", FieldFlags.Special | FieldFlags.Internal,
                    VeinCore.ValueTypeClass);
            (VeinCore.StringClass as RuntimeIshtarClass)
                !.DefineField("!!value", FieldFlags.Special | FieldFlags.Internal,
                    VeinCore.ValueTypeClass);

            (VeinCore.ArrayClass as RuntimeIshtarClass)
                !.DefineField("!!value", FieldFlags.Special,
                    VeinCore.ValueTypeClass);
            (VeinCore.ArrayClass as RuntimeIshtarClass)
                !.DefineField("!!block", FieldFlags.Special,
                    VeinCore.Int64Class);
            (VeinCore.ArrayClass as RuntimeIshtarClass)
                !.DefineField("!!size", FieldFlags.Special,
                    VeinCore.Int64Class);
            (VeinCore.ArrayClass as RuntimeIshtarClass)
                !.DefineField("!!rank", FieldFlags.Special,
                    VeinCore.Int64Class);
        }
    }

    public class InvalidSystemMappingException : Exception
    {
        public InvalidSystemMappingException() : base($"Incorrect initialization step.\n" +
                    $"Please report the problem into 'https://github.com/vein-lang/vein/issues'. ")
        { }
    }
}
