namespace wave.runtime
{
    using System;
    using static WaveTypeCode;

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
        public static bool IsCompatibleNumber(this WaveTypeCode variable, WaveTypeCode assign)
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
        public static bool HasFloat(this WaveTypeCode code)
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
        public static bool HasUnsigned(this WaveTypeCode code)
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
        public static bool HasSigned(this WaveTypeCode code)
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
        public static bool HasInteger(this WaveTypeCode code) =>
            HasSigned(code) || HasUnsigned(code);

        public static WaveClass AsClass(this WaveTypeCode code)
        {
            switch (code)
            {
                case TYPE_CHAR:
                    return WaveCore.CharClass;
                case TYPE_I1: // TODO
                case TYPE_U1:
                    return WaveCore.ByteClass;
                case TYPE_U2:
                    return WaveCore.UInt16Class;
                case TYPE_U4:
                    return WaveCore.UInt32Class;
                case TYPE_U8:
                    return WaveCore.UInt64Class;
                case TYPE_R8:
                    return WaveCore.DoubleClass;
                case TYPE_R4:
                    return WaveCore.FloatClass;
                case TYPE_R2:
                    return WaveCore.HalfClass;
                case TYPE_ARRAY:
                    return WaveCore.ArrayClass;
                case TYPE_BOOLEAN:
                    return WaveCore.BoolClass;
                case TYPE_NONE:
                case TYPE_CLASS:
                    throw new Exception();
                case TYPE_VOID:
                    return WaveCore.VoidClass;
                case TYPE_OBJECT:
                    return WaveCore.ObjectClass;
                case TYPE_I2:
                    return WaveCore.Int16Class;
                case TYPE_I4:
                    return WaveCore.Int32Class;
                case TYPE_I8:
                    return WaveCore.Int64Class;
                case TYPE_STRING:
                    return WaveCore.StringClass;
                default:
                    throw new ArgumentOutOfRangeException(nameof(code), code, null);
            }
        }
        
    }
}