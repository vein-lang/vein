namespace mana.ishtar.emit
{
    using System;
    using runtime;

    public class TypeForwarder
    {
        public static void Indicate(ManaClass clazz)
        {
            switch (clazz.FullName.NameWithNS)
            {
                case "global::mana/lang/Object":
                    clazz.TypeCode = ManaTypeCode.TYPE_OBJECT;
                    ManaCore.ObjectClass = clazz;
                    break;
                case "global::mana/lang/ValueType":
                    clazz.TypeCode = ManaTypeCode.TYPE_OBJECT;
                    ManaCore.ValueTypeClass = clazz;
                    break;
                case "global::mana/lang/Void":
                    clazz.TypeCode = ManaTypeCode.TYPE_VOID;
                    ManaCore.VoidClass = clazz;
                    break;
                case "global::mana/lang/Int64":
                    clazz.TypeCode = ManaTypeCode.TYPE_I8;
                    ManaCore.Int64Class = clazz;
                    break;
                case "global::mana/lang/Int32":
                    clazz.TypeCode = ManaTypeCode.TYPE_I4;
                    ManaCore.Int32Class = clazz;
                    break;
                case "global::mana/lang/Int16":
                    clazz.TypeCode = ManaTypeCode.TYPE_I2;
                    ManaCore.Int16Class = clazz;
                    break;
                case "global::mana/lang/UInt64":
                    clazz.TypeCode = ManaTypeCode.TYPE_U8;
                    ManaCore.UInt64Class = clazz;
                    break;
                case "global::mana/lang/UInt32":
                    clazz.TypeCode = ManaTypeCode.TYPE_U4;
                    ManaCore.UInt32Class = clazz;
                    break;
                case "global::mana/lang/UInt16":
                    clazz.TypeCode = ManaTypeCode.TYPE_U2;
                    ManaCore.UInt16Class = clazz;
                    break;
                case "global::mana/lang/Boolean":
                    clazz.TypeCode = ManaTypeCode.TYPE_BOOLEAN;
                    ManaCore.BoolClass = clazz;
                    break;
                case "global::mana/lang/String":
                    clazz.TypeCode = ManaTypeCode.TYPE_STRING;
                    ManaCore.StringClass = clazz;
                    break;
                case "global::mana/lang/Char":
                    clazz.TypeCode = ManaTypeCode.TYPE_CHAR;
                    ManaCore.CharClass = clazz;
                    break;
                case "global::mana/lang/Half":
                    clazz.TypeCode = ManaTypeCode.TYPE_R2;
                    ManaCore.HalfClass = clazz;
                    break;
                case "global::mana/lang/Float":
                    clazz.TypeCode = ManaTypeCode.TYPE_R4;
                    ManaCore.FloatClass = clazz;
                    break;
                case "global::mana/lang/Double":
                    clazz.TypeCode = ManaTypeCode.TYPE_R8;
                    ManaCore.DoubleClass = clazz;
                    break;
                case "global::mana/lang/Decimal":
                    clazz.TypeCode = ManaTypeCode.TYPE_R16;
                    ManaCore.DecimalClass = clazz;
                    break;
                case "global::mana/lang/Byte":
                    clazz.TypeCode = ManaTypeCode.TYPE_U1;
                    ManaCore.ByteClass = clazz;
                    break;
                case "global::mana/lang/Exception":
                    clazz.TypeCode = ManaTypeCode.TYPE_CLASS;
                    ManaCore.ExceptionClass = clazz;
                    break;
                default:
                    throw new NotSupportedException();
            }
        }
    }
}