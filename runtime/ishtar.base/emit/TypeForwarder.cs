namespace ishtar.emit
{
    using System;
    using vein.runtime;

    public class TypeForwarder
    {
        public static void Indicate(VeinClass clazz)
        {
            switch (clazz.FullName.NameWithNS)
            {
                case "global::vein/lang/Aspect":
                    clazz.TypeCode = VeinTypeCode.TYPE_CLASS;
                    VeinCore.AspectClass = clazz;
                    break;
                case "global::vein/lang/Raw":
                    clazz.TypeCode = VeinTypeCode.TYPE_RAW;
                    VeinCore.RawClass = clazz;
                    break;
                case "global::vein/lang/Object":
                    clazz.TypeCode = VeinTypeCode.TYPE_OBJECT;
                    VeinCore.ObjectClass = clazz;
                    break;
                case "global::vein/lang/ValueType":
                    clazz.TypeCode = VeinTypeCode.TYPE_OBJECT;
                    VeinCore.ValueTypeClass = clazz;
                    break;
                case "global::vein/lang/Array":
                    clazz.TypeCode = VeinTypeCode.TYPE_ARRAY;
                    VeinCore.ArrayClass = clazz;
                    break;
                case "global::vein/lang/Void":
                    clazz.TypeCode = VeinTypeCode.TYPE_VOID;
                    VeinCore.VoidClass = clazz;
                    break;
                case "global::vein/lang/Int64":
                    clazz.TypeCode = VeinTypeCode.TYPE_I8;
                    VeinCore.Int64Class = clazz;
                    break;
                case "global::vein/lang/Int32":
                    clazz.TypeCode = VeinTypeCode.TYPE_I4;
                    VeinCore.Int32Class = clazz;
                    break;
                case "global::vein/lang/Int16":
                    clazz.TypeCode = VeinTypeCode.TYPE_I2;
                    VeinCore.Int16Class = clazz;
                    break;
                case "global::vein/lang/UInt64":
                    clazz.TypeCode = VeinTypeCode.TYPE_U8;
                    VeinCore.UInt64Class = clazz;
                    break;
                case "global::vein/lang/UInt32":
                    clazz.TypeCode = VeinTypeCode.TYPE_U4;
                    VeinCore.UInt32Class = clazz;
                    break;
                case "global::vein/lang/UInt16":
                    clazz.TypeCode = VeinTypeCode.TYPE_U2;
                    VeinCore.UInt16Class = clazz;
                    break;
                case "global::vein/lang/Boolean":
                    clazz.TypeCode = VeinTypeCode.TYPE_BOOLEAN;
                    VeinCore.BoolClass = clazz;
                    break;
                case "global::vein/lang/String":
                    clazz.TypeCode = VeinTypeCode.TYPE_STRING;
                    VeinCore.StringClass = clazz;
                    break;
                case "global::vein/lang/Char":
                    clazz.TypeCode = VeinTypeCode.TYPE_CHAR;
                    VeinCore.CharClass = clazz;
                    break;
                case "global::vein/lang/Half":
                    clazz.TypeCode = VeinTypeCode.TYPE_R2;
                    VeinCore.HalfClass = clazz;
                    break;
                case "global::vein/lang/Float":
                    clazz.TypeCode = VeinTypeCode.TYPE_R4;
                    VeinCore.FloatClass = clazz;
                    break;
                case "global::vein/lang/Double":
                    clazz.TypeCode = VeinTypeCode.TYPE_R8;
                    VeinCore.DoubleClass = clazz;
                    break;
                case "global::vein/lang/Decimal":
                    clazz.TypeCode = VeinTypeCode.TYPE_R16;
                    VeinCore.DecimalClass = clazz;
                    break;
                case "global::vein/lang/Byte":
                    clazz.TypeCode = VeinTypeCode.TYPE_U1;
                    VeinCore.ByteClass = clazz;
                    break;
                case "global::vein/lang/Exception":
                    clazz.TypeCode = VeinTypeCode.TYPE_CLASS;
                    VeinCore.ExceptionClass = clazz;
                    break;
                default:
                    throw new NotSupportedException();
            }
        }
    }
}
