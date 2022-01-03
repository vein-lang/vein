namespace vein.runtime;


using System;
using static VeinTypeCode;

public enum VeinTypeCode
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
    TYPE_STRING, /* string */
    TYPE_CLASS, /* custom class */
    TYPE_ARRAY, /* Array<?> */
    TYPE_TOKEN, /* type token */
    TYPE_RAW, /* raw pointer */
    TYPE_FUNCTION /* function class */
}

public static class VeinTypeCodeEx
{
    public static TypeCode ToCLRTypeCode(this VeinTypeCode type_code) => type_code switch
    {
        TYPE_BOOLEAN => TypeCode.Boolean,
        TYPE_CHAR => TypeCode.Char,
        TYPE_I1 => TypeCode.SByte,
        TYPE_U1 => TypeCode.Byte,
        TYPE_I2 => TypeCode.Int16,
        TYPE_U2 => TypeCode.UInt16,
        TYPE_I4 => TypeCode.Int32,
        TYPE_U4 => TypeCode.UInt32,
        TYPE_I8 => TypeCode.Int64,
        TYPE_U8 => TypeCode.UInt64,
        TYPE_R4 => TypeCode.Single,
        TYPE_R8 => TypeCode.Double,
        TYPE_R16 => TypeCode.Decimal,
        TYPE_STRING => TypeCode.String,
        _ => throw new NotSupportedException($"'{type_code}' cant convert to CLR type code.")
    };

    public static VeinTypeCode DetermineTypeCode<T>(this T value)
    {
        var clr_code = Type.GetTypeCode(value.GetType());

        return clr_code switch
        {
            TypeCode.Boolean => TYPE_BOOLEAN,
            TypeCode.Char => TYPE_CHAR,
            TypeCode.SByte => TYPE_I1,
            TypeCode.Byte => TYPE_U1,
            TypeCode.Int16 => TYPE_I2,
            TypeCode.UInt16 => TYPE_U2,
            TypeCode.Int32 => TYPE_I4,
            TypeCode.UInt32 => TYPE_U4,
            TypeCode.Int64 => TYPE_I8,
            TypeCode.UInt64 => TYPE_U8,
            TypeCode.Single => TYPE_R4,
            TypeCode.Double => TYPE_R8,
            TypeCode.Decimal => TYPE_R16,
            TypeCode.String => TYPE_STRING,
            _ => throw new NotSupportedException(
                $"'{clr_code}', '{value}', '{value.GetType()}' cant convert to Ishtar type code.")
        };
    }

    public static bool IsCompatibleNumber(this VeinTypeCode variable, VeinTypeCode assign)
    {
        if ((!variable.HasInteger() || !assign.HasInteger()) && (!variable.HasFloat() || !assign.HasFloat()))
            return false;
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
    public static bool HasFloat(this VeinTypeCode code) => code switch
    {
        TYPE_R2 => true,
        TYPE_R4 => true,
        TYPE_R8 => true,
        TYPE_R16 => true,
        _ => false
    };

    public static bool HasUnsigned(this VeinTypeCode code) => code switch
    {
        TYPE_U1 => true,
        TYPE_U2 => true,
        TYPE_U4 => true,
        TYPE_U8 => true,
        _ => false
    };

    public static bool HasSigned(this VeinTypeCode code) => code switch
    {
        TYPE_I1 => true,
        TYPE_I2 => true,
        TYPE_I4 => true,
        TYPE_I8 => true,
        _ => false
    };

    public static bool HasInteger(this VeinTypeCode code) =>
        HasSigned(code) || HasUnsigned(code);

    public static bool HasNumber(this VeinTypeCode code) =>
        HasInteger(code) || HasFloat(code);

    public static VeinClass AsClass(this VeinTypeCode code) => code switch
    {
        TYPE_CHAR => VeinCore.CharClass,
        TYPE_I1 => VeinCore.SByteClass,
        TYPE_U1 => VeinCore.ByteClass,
        TYPE_U2 => VeinCore.UInt16Class,
        TYPE_U4 => VeinCore.UInt32Class,
        TYPE_U8 => VeinCore.UInt64Class,
        TYPE_R8 => VeinCore.DoubleClass,
        TYPE_R4 => VeinCore.FloatClass,
        TYPE_R2 => VeinCore.HalfClass,
        TYPE_ARRAY => VeinCore.ArrayClass,
        TYPE_BOOLEAN => VeinCore.BoolClass,
        TYPE_VOID => VeinCore.VoidClass,
        TYPE_OBJECT => VeinCore.ObjectClass,
        TYPE_I2 => VeinCore.Int16Class,
        TYPE_I4 => VeinCore.Int32Class,
        TYPE_I8 => VeinCore.Int64Class,
        TYPE_STRING => VeinCore.StringClass,
        TYPE_FUNCTION => VeinCore.FunctionClass,
        _ => throw new ArgumentOutOfRangeException(nameof(code), code, null)
    };
}
