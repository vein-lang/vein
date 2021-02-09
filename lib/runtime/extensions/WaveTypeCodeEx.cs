namespace wave.emit
{
    using System;

    public static class WaveTypeCodeEx
    {
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