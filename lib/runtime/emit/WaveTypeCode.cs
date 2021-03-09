namespace wave.emit
{
    using System;

    public enum WaveTypeCode
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

    public static class WaveTypeCodeEx
    {
        public static bool IsCompatibleNumber(this WaveTypeCode code, WaveTypeCode target)
        {
            if (code.HasFloat() && target.HasFloat())
                return code >= target;
            if (code.HasFloat())
            {
                if (code == WaveTypeCode.TYPE_R2)
                    return target <= WaveTypeCode.TYPE_U2;
                if (code == WaveTypeCode.TYPE_R4)
                    return target <= WaveTypeCode.TYPE_U4;
                return target <= WaveTypeCode.TYPE_U8;
            }
            return IsCompatibleInteger(code, target);
        }
        public static bool IsCompatibleInteger(this WaveTypeCode code, WaveTypeCode target)
        {
            if (!code.HasInteger())
                return false;
            if (!target.HasInteger())
                return false;
            if (code.HasUnsigned() && target.HasUnsigned())
                return code >= target;
            if (code.HasSigned() && target.HasSigned())
                return code >= target;
            return false;
        }
        public static bool HasFloat(this WaveTypeCode code)
        {
            switch (code)
            {
                case WaveTypeCode.TYPE_R2:
                case WaveTypeCode.TYPE_R4:
                case WaveTypeCode.TYPE_R8:
                case WaveTypeCode.TYPE_R16:
                    return true;
                default:
                    return false;
            }
        }
        public static bool HasUnsigned(this WaveTypeCode code)
        {
            switch (code)
            {
                case WaveTypeCode.TYPE_U1:
                case WaveTypeCode.TYPE_U2:
                case WaveTypeCode.TYPE_U4:
                case WaveTypeCode.TYPE_U8:
                    return true;
                default:
                    return false;
            }
        }
        public static bool HasSigned(this WaveTypeCode code)
        {
            switch (code)
            {
                case WaveTypeCode.TYPE_I1:
                case WaveTypeCode.TYPE_I2:
                case WaveTypeCode.TYPE_I4:
                case WaveTypeCode.TYPE_I8:
                    return true;
                default:
                    return false;
            }
        }
        public static bool HasInteger(this WaveTypeCode code) =>
            HasSigned(code) || HasUnsigned(code);

        public static WaveType AsType(this WaveTypeCode code)
        {
            WaveCore.Types.Init();
            switch (code)
            {
                case WaveTypeCode.TYPE_CHAR:
                    return WaveCore.Types.CharType;
                case WaveTypeCode.TYPE_I1:
                case WaveTypeCode.TYPE_U1:
                case WaveTypeCode.TYPE_U2:
                case WaveTypeCode.TYPE_U4:
                case WaveTypeCode.TYPE_U8:
                case WaveTypeCode.TYPE_R8:
                    return WaveCore.Types.DoubleType;
                case WaveTypeCode.TYPE_R4:
                    return WaveCore.Types.FloatType;
                case WaveTypeCode.TYPE_ARRAY:
                    return WaveCore.Types.ArrayType;
                case WaveTypeCode.TYPE_BOOLEAN:
                    return WaveCore.Types.BoolType;
                case WaveTypeCode.TYPE_NONE:
                case WaveTypeCode.TYPE_CLASS:
                    throw new Exception();
                case WaveTypeCode.TYPE_VOID:
                    return WaveCore.Types.VoidType;
                case WaveTypeCode.TYPE_OBJECT:
                    return WaveCore.Types.ObjectType;
                case WaveTypeCode.TYPE_I2:
                    return WaveCore.Types.Int16Type;
                case WaveTypeCode.TYPE_I4:
                    return WaveCore.Types.Int32Type;
                case WaveTypeCode.TYPE_I8:
                    return WaveCore.Types.Int64Type;
                case WaveTypeCode.TYPE_STRING:
                    return WaveCore.Types.StringType;
                default:
                    throw new ArgumentOutOfRangeException(nameof(code), code, null);
            }
        }
    }
}