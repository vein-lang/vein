namespace ishtar.emit
{
    using System;
    using vein.runtime;

    public class TypeForwarder
    {
        public static void Indicate(VeinCore types, VeinClass clazz)
        {
            switch (clazz.FullName.NameWithNS)
            {
                case "std/Aspect":
                    clazz.TypeCode = VeinTypeCode.TYPE_CLASS;
                    types.AspectClass = clazz;
                    break;
                case "std/Raw":
                    clazz.TypeCode = VeinTypeCode.TYPE_RAW;
                    types.RawClass = clazz;
                    break;
                case "std/Object":
                    clazz.TypeCode = VeinTypeCode.TYPE_OBJECT;
                    types.ObjectClass = clazz;
                    break;
                case "std/ValueType":
                    clazz.TypeCode = VeinTypeCode.TYPE_OBJECT;
                    types.ValueTypeClass = clazz;
                    break;
                case "std/Array":
                    clazz.TypeCode = VeinTypeCode.TYPE_ARRAY;
                    types.ArrayClass = clazz;
                    break;
                case "std/Void":
                    clazz.TypeCode = VeinTypeCode.TYPE_VOID;
                    types.VoidClass = clazz;
                    break;
                case "std/Int64":
                    clazz.TypeCode = VeinTypeCode.TYPE_I8;
                    types.Int64Class = clazz;
                    break;
                case "std/Int32":
                    clazz.TypeCode = VeinTypeCode.TYPE_I4;
                    types.Int32Class = clazz;
                    break;
                case "std/Int16":
                    clazz.TypeCode = VeinTypeCode.TYPE_I2;
                    types.Int16Class = clazz;
                    break;
                case "std/UInt64":
                    clazz.TypeCode = VeinTypeCode.TYPE_U8;
                    types.UInt64Class = clazz;
                    break;
                case "std/UInt32":
                    clazz.TypeCode = VeinTypeCode.TYPE_U4;
                    types.UInt32Class = clazz;
                    break;
                case "std/UInt16":
                    clazz.TypeCode = VeinTypeCode.TYPE_U2;
                    types.UInt16Class = clazz;
                    break;
                case "std/Boolean":
                    clazz.TypeCode = VeinTypeCode.TYPE_BOOLEAN;
                    types.BoolClass = clazz;
                    break;
                case "std/String":
                    clazz.TypeCode = VeinTypeCode.TYPE_STRING;
                    types.StringClass = clazz;
                    break;
                case "std/Char":
                    clazz.TypeCode = VeinTypeCode.TYPE_CHAR;
                    types.CharClass = clazz;
                    break;
                case "std/Half":
                    clazz.TypeCode = VeinTypeCode.TYPE_R2;
                    types.HalfClass = clazz;
                    break;
                case "std/Float":
                    clazz.TypeCode = VeinTypeCode.TYPE_R4;
                    types.FloatClass = clazz;
                    break;
                case "std/Double":
                    clazz.TypeCode = VeinTypeCode.TYPE_R8;
                    types.DoubleClass = clazz;
                    break;
                case "std/Decimal":
                    clazz.TypeCode = VeinTypeCode.TYPE_R16;
                    types.DecimalClass = clazz;
                    break;
                case "std/Byte":
                    clazz.TypeCode = VeinTypeCode.TYPE_U1;
                    types.ByteClass = clazz;
                    break;
                case "std/Exception":
                    clazz.TypeCode = VeinTypeCode.TYPE_CLASS;
                    types.ExceptionClass = clazz;
                    break;
                default:
                    throw new NotSupportedException();
            }
        }
    }
}
