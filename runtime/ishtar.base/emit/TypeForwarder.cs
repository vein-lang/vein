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
                case "global::vein/lang/Aspect":
                    clazz.TypeCode = VeinTypeCode.TYPE_CLASS;
                    types.AspectClass = clazz;
                    break;
                case "global::vein/lang/Raw":
                    clazz.TypeCode = VeinTypeCode.TYPE_RAW;
                    types.RawClass = clazz;
                    break;
                case "global::vein/lang/Object":
                    clazz.TypeCode = VeinTypeCode.TYPE_OBJECT;
                    types.ObjectClass = clazz;
                    break;
                case "global::vein/lang/ValueType":
                    clazz.TypeCode = VeinTypeCode.TYPE_OBJECT;
                    types.ValueTypeClass = clazz;
                    break;
                case "global::vein/lang/Array":
                    clazz.TypeCode = VeinTypeCode.TYPE_ARRAY;
                    types.ArrayClass = clazz;
                    break;
                case "global::vein/lang/Void":
                    clazz.TypeCode = VeinTypeCode.TYPE_VOID;
                    types.VoidClass = clazz;
                    break;
                case "global::vein/lang/Int64":
                    clazz.TypeCode = VeinTypeCode.TYPE_I8;
                    types.Int64Class = clazz;
                    break;
                case "global::vein/lang/Int32":
                    clazz.TypeCode = VeinTypeCode.TYPE_I4;
                    types.Int32Class = clazz;
                    break;
                case "global::vein/lang/Int16":
                    clazz.TypeCode = VeinTypeCode.TYPE_I2;
                    types.Int16Class = clazz;
                    break;
                case "global::vein/lang/UInt64":
                    clazz.TypeCode = VeinTypeCode.TYPE_U8;
                    types.UInt64Class = clazz;
                    break;
                case "global::vein/lang/UInt32":
                    clazz.TypeCode = VeinTypeCode.TYPE_U4;
                    types.UInt32Class = clazz;
                    break;
                case "global::vein/lang/UInt16":
                    clazz.TypeCode = VeinTypeCode.TYPE_U2;
                    types.UInt16Class = clazz;
                    break;
                case "global::vein/lang/Boolean":
                    clazz.TypeCode = VeinTypeCode.TYPE_BOOLEAN;
                    types.BoolClass = clazz;
                    break;
                case "global::vein/lang/String":
                    clazz.TypeCode = VeinTypeCode.TYPE_STRING;
                    types.StringClass = clazz;
                    break;
                case "global::vein/lang/Char":
                    clazz.TypeCode = VeinTypeCode.TYPE_CHAR;
                    types.CharClass = clazz;
                    break;
                case "global::vein/lang/Half":
                    clazz.TypeCode = VeinTypeCode.TYPE_R2;
                    types.HalfClass = clazz;
                    break;
                case "global::vein/lang/Float":
                    clazz.TypeCode = VeinTypeCode.TYPE_R4;
                    types.FloatClass = clazz;
                    break;
                case "global::vein/lang/Double":
                    clazz.TypeCode = VeinTypeCode.TYPE_R8;
                    types.DoubleClass = clazz;
                    break;
                case "global::vein/lang/Decimal":
                    clazz.TypeCode = VeinTypeCode.TYPE_R16;
                    types.DecimalClass = clazz;
                    break;
                case "global::vein/lang/Byte":
                    clazz.TypeCode = VeinTypeCode.TYPE_U1;
                    types.ByteClass = clazz;
                    break;
                case "global::vein/lang/Exception":
                    clazz.TypeCode = VeinTypeCode.TYPE_CLASS;
                    types.ExceptionClass = clazz;
                    break;
                default:
                    throw new NotSupportedException();
            }
        }
    }
}
