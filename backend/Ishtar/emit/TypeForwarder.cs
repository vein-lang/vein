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
                    ManaCore.ObjectClass = clazz;
                    break;
                case "global::mana/lang/ValueType":
                    ManaCore.ValueTypeClass = clazz;
                    break;
                case "global::mana/lang/Void":
                    ManaCore.VoidClass = clazz;
                    break;
                case "global::mana/lang/Int64":
                    ManaCore.Int64Class = clazz;
                    break;
                case "global::mana/lang/Int32":
                    ManaCore.Int32Class = clazz;
                    break;
                case "global::mana/lang/Int16":
                    ManaCore.Int16Class = clazz;
                    break;
                case "global::mana/lang/UInt64":
                    ManaCore.UInt64Class = clazz;
                    break;
                case "global::mana/lang/UInt32":
                    ManaCore.UInt32Class = clazz;
                    break;
                case "global::mana/lang/UInt16":
                    ManaCore.UInt16Class = clazz;
                    break;
                case "global::mana/lang/Boolean":
                    ManaCore.BoolClass = clazz;
                    break;
                case "global::mana/lang/String":
                    ManaCore.StringClass = clazz;
                    break;
                case "global::mana/lang/Char":
                    ManaCore.CharClass = clazz;
                    break;
                case "global::mana/lang/Half":
                    ManaCore.HalfClass = clazz;
                    break;
                case "global::mana/lang/Float":
                    ManaCore.FloatClass = clazz;
                    break;
                case "global::mana/lang/Double":
                    ManaCore.DoubleClass = clazz;
                    break;
                case "global::mana/lang/Decimal":
                    ManaCore.DecimalClass = clazz;
                    break;
                case "global::mana/lang/Byte":
                    ManaCore.ByteClass = clazz;
                    break;
                case "global::mana/lang/Exception":
                    ManaCore.ExceptionClass = clazz;
                    break;
                default:
                    throw new NotSupportedException();
            }
        }
    }
}