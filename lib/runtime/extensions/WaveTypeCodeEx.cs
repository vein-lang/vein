namespace wave.emit
{
    using System;

    public static class WaveTypeCodeEx
    {
        public static WaveRuntimeType AsType(this WaveTypeCode code)
        {
            switch (code)
            {
                case WaveTypeCode.TYPE_CHAR:
                case WaveTypeCode.TYPE_I1:
                case WaveTypeCode.TYPE_U1:
                case WaveTypeCode.TYPE_U2:
                case WaveTypeCode.TYPE_U4:
                case WaveTypeCode.TYPE_U8:
                case WaveTypeCode.TYPE_R8:
                case WaveTypeCode.TYPE_R4:
                case WaveTypeCode.TYPE_ARRAY:
                case WaveTypeCode.TYPE_BOOLEAN:
                case WaveTypeCode.TYPE_NONE:
                case WaveTypeCode.TYPE_CLASS:
                    throw new Exception();
                case WaveTypeCode.TYPE_VOID:
                    return WaveCore.VoidClass.AsType(code);
                case WaveTypeCode.TYPE_OBJECT:
                    return WaveCore.ObjectClass.AsType(code);
                case WaveTypeCode.TYPE_I2:
                    return WaveCore.Int16Class.AsType(code);
                case WaveTypeCode.TYPE_I4:
                    return WaveCore.Int32Class.AsType(code);
                case WaveTypeCode.TYPE_I8:
                    return WaveCore.Int64Class.AsType(code);
                case WaveTypeCode.TYPE_STRING:
                    return WaveCore.StringClass.AsType(code);
                default:
                    throw new ArgumentOutOfRangeException(nameof(code), code, null);
            }
        }
    }
}