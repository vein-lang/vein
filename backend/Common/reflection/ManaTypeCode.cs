namespace mana.runtime
{
    using System;
    using System.Reflection;
    using exceptions;
    using static ManaTypeCode;

    public enum ManaTypeCode
    {
        TYPE_NONE = 0x0,
        TYPE_VOID,
        TYPE_OBJECT,
        TYPE_BOOLEAN,
        TYPE_CHAR,
        TYPE_I1, /* sbyte  */
        TYPE_U1, /* byte   */
        TYPE_I2, /* short  */
        TYPE_U2, /* ushort */
        TYPE_I4, /* int32  */
        TYPE_U4, /* uint32 */
        TYPE_I8, /* long   */
        TYPE_U8, /* ulong  */
        TYPE_R2, /* half  */
        TYPE_R4, /* float  */
        TYPE_R8, /* double */
        TYPE_R16, /* decimal */
        TYPE_STRING,
        TYPE_CLASS,
        TYPE_ARRAY
    }

    public static class ManaTypeCodeEx
    {
        public static TypeCode ToCLRTypeCode(this ManaTypeCode type_code)
        {
            switch (type_code)
            {
                case TYPE_BOOLEAN:
                    return TypeCode.Boolean;
                case TYPE_CHAR:
                    return TypeCode.Char;
                case TYPE_I1:
                    return TypeCode.SByte;
                case TYPE_U1:
                    return TypeCode.Byte;
                case TYPE_I2:
                    return TypeCode.Int16;
                case TYPE_U2:
                    return TypeCode.UInt16;
                case TYPE_I4:
                    return TypeCode.Int32;
                case TYPE_U4:
                    return TypeCode.UInt32;
                case TYPE_I8:
                    return TypeCode.Int64;
                case TYPE_U8:
                    return TypeCode.UInt64;
                case TYPE_R4:
                    return TypeCode.Single;
                case TYPE_R8:
                    return TypeCode.Double;
                case TYPE_R16:
                    return TypeCode.Decimal;
                case TYPE_STRING:
                    return TypeCode.String;
            }

            throw new NotSupportedException($"'{type_code}' cant convert to CLR type code.");
        }
        public static ManaTypeCode DetermineTypeCode<T>(this T value)
        {
            var clr_code = Type.GetTypeCode(value.GetType());

            switch (clr_code)
            {
                case TypeCode.Boolean:
                    return TYPE_BOOLEAN;
                case TypeCode.Char:
                    return TYPE_CHAR;
                case TypeCode.SByte:
                    return TYPE_I1;
                case TypeCode.Byte:
                    return TYPE_U1;
                case TypeCode.Int16:
                    return TYPE_I2;
                case TypeCode.UInt16:
                    return TYPE_U2;
                case TypeCode.Int32:
                    return TYPE_I4;
                case TypeCode.UInt32:
                    return TYPE_U4;
                case TypeCode.Int64:
                    return TYPE_I8;
                case TypeCode.UInt64:
                    return TYPE_U8;
                case TypeCode.Single:
                    return TYPE_R4;
                case TypeCode.Double:
                    return TYPE_R8;
                case TypeCode.Decimal:
                    return TYPE_R16;
                case TypeCode.String:
                    return TYPE_STRING;
            }
            throw new NotSupportedException($"'{clr_code}', '{value}', '{value.GetType()}' cant convert to Ishtar type code.");
        }

        public static bool IsCompatibleNumber(this ManaTypeCode variable, ManaTypeCode assign)
        {
            if (variable == assign)
                return true;
            if (variable.HasFloat() && assign.HasInteger())
                return true;
            return (variable, assign) switch
            {
                (TYPE_R4, TYPE_R8) => false,
                (TYPE_R4, TYPE_R16) => false,
                (TYPE_R4, TYPE_R2) => true,

                (TYPE_R8, TYPE_R4) => true,
                (TYPE_R8, TYPE_R16) => false,
                (TYPE_R8, TYPE_R2) => false,

                (TYPE_R16, TYPE_R8) => false,
                (TYPE_R16, TYPE_R4) => false,
                (TYPE_R16, TYPE_R2) => false,

                (TYPE_U4, TYPE_I1) => true,
                (TYPE_U4, TYPE_I2) => true,
                (TYPE_U4, TYPE_I4) => true,
                (TYPE_U4, TYPE_I8) => false,

                (TYPE_U2, TYPE_I1) => true,
                (TYPE_U2, TYPE_I2) => true,
                (TYPE_U2, TYPE_I4) => false,
                (TYPE_U2, TYPE_I8) => false,


                (TYPE_U1, TYPE_I1) => true,
                (TYPE_U1, TYPE_I2) => false,
                (TYPE_U1, TYPE_I4) => false,
                (TYPE_U1, TYPE_I8) => false,


                (TYPE_U8, TYPE_I1) => true,
                (TYPE_U8, TYPE_I2) => true,
                (TYPE_U8, TYPE_I4) => true,
                (TYPE_U8, TYPE_I8) => true,

                (TYPE_I8, TYPE_U1) => true,
                (TYPE_I8, TYPE_U2) => true,
                (TYPE_I8, TYPE_U4) => true,
                (TYPE_I8, TYPE_U8) => false,


                (TYPE_I4, TYPE_U1) => true,
                (TYPE_I4, TYPE_U2) => true,
                (TYPE_I4, TYPE_U4) => false,
                (TYPE_I4, TYPE_U8) => false,

                (TYPE_I2, TYPE_U1) => true,
                (TYPE_I2, TYPE_U2) => false,
                (TYPE_I2, TYPE_U4) => false,
                (TYPE_I2, TYPE_U8) => false,


                (TYPE_I1, TYPE_U1) => false,
                (TYPE_I1, TYPE_U2) => false,
                (TYPE_I1, TYPE_U4) => false,
                (TYPE_I1, TYPE_U8) => false,

                // 

                (TYPE_I1, TYPE_I1) => true,
                (TYPE_I1, TYPE_I2) => false,
                (TYPE_I1, TYPE_I4) => false,
                (TYPE_I1, TYPE_I8) => false,


                (TYPE_I2, TYPE_I1) => false,
                (TYPE_I2, TYPE_I2) => true,
                (TYPE_I2, TYPE_I4) => false,
                (TYPE_I2, TYPE_I8) => false,


                (TYPE_I4, TYPE_I1) => true,
                (TYPE_I4, TYPE_I2) => true,
                (TYPE_I4, TYPE_I4) => true,
                (TYPE_I4, TYPE_I8) => false,

                (TYPE_I8, TYPE_I1) => true,
                (TYPE_I8, TYPE_I2) => true,
                (TYPE_I8, TYPE_I4) => true,
                (TYPE_I8, TYPE_I8) => true,


                _ => false
            };
        }
        public static bool HasFloat(this ManaTypeCode code)
        {
            switch (code)
            {
                case TYPE_R2:
                case TYPE_R4:
                case TYPE_R8:
                case TYPE_R16:
                    return true;
                default:
                    return false;
            }
        }
        public static bool HasUnsigned(this ManaTypeCode code)
        {
            switch (code)
            {
                case TYPE_U1:
                case TYPE_U2:
                case TYPE_U4:
                case TYPE_U8:
                    return true;
                default:
                    return false;
            }
        }
        public static bool HasSigned(this ManaTypeCode code)
        {
            switch (code)
            {
                case TYPE_I1:
                case TYPE_I2:
                case TYPE_I4:
                case TYPE_I8:
                    return true;
                default:
                    return false;
            }
        }
        public static bool HasInteger(this ManaTypeCode code) =>
            HasSigned(code) || HasUnsigned(code);

        public static ManaClass AsClass(this ManaTypeCode code)
        {
            switch (code)
            {
                case TYPE_CHAR:
                    return ManaCore.CharClass;
                case TYPE_I1: // TODO
                case TYPE_U1:
                    return ManaCore.ByteClass;
                case TYPE_U2:
                    return ManaCore.UInt16Class;
                case TYPE_U4:
                    return ManaCore.UInt32Class;
                case TYPE_U8:
                    return ManaCore.UInt64Class;
                case TYPE_R8:
                    return ManaCore.DoubleClass;
                case TYPE_R4:
                    return ManaCore.FloatClass;
                case TYPE_R2:
                    return ManaCore.HalfClass;
                case TYPE_ARRAY:
                    return ManaCore.ArrayClass;
                case TYPE_BOOLEAN:
                    return ManaCore.BoolClass;
                case TYPE_NONE:
                case TYPE_CLASS:
                    throw new Exception();
                case TYPE_VOID:
                    return ManaCore.VoidClass;
                case TYPE_OBJECT:
                    return ManaCore.ObjectClass;
                case TYPE_I2:
                    return ManaCore.Int16Class;
                case TYPE_I4:
                    return ManaCore.Int32Class;
                case TYPE_I8:
                    return ManaCore.Int64Class;
                case TYPE_STRING:
                    return ManaCore.StringClass;
                default:
                    throw new ArgumentOutOfRangeException(nameof(code), code, null);
            }
        }

    }
}