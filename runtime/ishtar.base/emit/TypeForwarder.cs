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
                case "global::std/Aspect":
                    clazz.TypeCode = VeinTypeCode.TYPE_CLASS;
                    types.AspectClass = clazz;
                    break;
                case "global::std/Raw":
                    clazz.TypeCode = VeinTypeCode.TYPE_RAW;
                    types.RawClass = clazz;
                    break;
                case "global::std/Object":
                    clazz.TypeCode = VeinTypeCode.TYPE_OBJECT;
                    types.ObjectClass = clazz;
                    break;
                case "global::std/ValueType":
                    clazz.TypeCode = VeinTypeCode.TYPE_OBJECT;
                    types.ValueTypeClass = clazz;
                    break;
                case "global::std/Array":
                    clazz.TypeCode = VeinTypeCode.TYPE_ARRAY;
                    types.ArrayClass = clazz;
                    break;
                case "global::std/Void":
                    clazz.TypeCode = VeinTypeCode.TYPE_VOID;
                    types.VoidClass = clazz;
                    break;
                case "global::std/Int64":
                    clazz.TypeCode = VeinTypeCode.TYPE_I8;
                    types.Int64Class = clazz;
                    break;
                case "global::std/Int32":
                    clazz.TypeCode = VeinTypeCode.TYPE_I4;
                    types.Int32Class = clazz;
                    break;
                case "global::std/Int16":
                    clazz.TypeCode = VeinTypeCode.TYPE_I2;
                    types.Int16Class = clazz;
                    break;
                case "global::std/UInt64":
                    clazz.TypeCode = VeinTypeCode.TYPE_U8;
                    types.UInt64Class = clazz;
                    break;
                case "global::std/UInt32":
                    clazz.TypeCode = VeinTypeCode.TYPE_U4;
                    types.UInt32Class = clazz;
                    break;
                case "global::std/UInt16":
                    clazz.TypeCode = VeinTypeCode.TYPE_U2;
                    types.UInt16Class = clazz;
                    break;
                case "global::std/Boolean":
                    clazz.TypeCode = VeinTypeCode.TYPE_BOOLEAN;
                    types.BoolClass = clazz;
                    break;
                case "global::std/String":
                    clazz.TypeCode = VeinTypeCode.TYPE_STRING;
                    types.StringClass = clazz;
                    break;
                case "global::std/Char":
                    clazz.TypeCode = VeinTypeCode.TYPE_CHAR;
                    types.CharClass = clazz;
                    break;
                case "global::std/Half":
                    clazz.TypeCode = VeinTypeCode.TYPE_R2;
                    types.HalfClass = clazz;
                    break;
                case "global::std/Float":
                    clazz.TypeCode = VeinTypeCode.TYPE_R4;
                    types.FloatClass = clazz;
                    break;
                case "global::std/Double":
                    clazz.TypeCode = VeinTypeCode.TYPE_R8;
                    types.DoubleClass = clazz;
                    break;
                case "global::std/Decimal":
                    clazz.TypeCode = VeinTypeCode.TYPE_R16;
                    types.DecimalClass = clazz;
                    break;
                case "global::std/Byte":
                    clazz.TypeCode = VeinTypeCode.TYPE_U1;
                    types.ByteClass = clazz;
                    break;
                case "global::std/Exception":
                    clazz.TypeCode = VeinTypeCode.TYPE_CLASS;
                    types.ExceptionClass = clazz;
                    break;
                default:
                    throw new NotSupportedException();
            }
        }
    }
}
