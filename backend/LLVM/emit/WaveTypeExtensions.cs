namespace wave.llvm.emit
{
    using System;
    using LLVMSharp;
    using runtime;

    public static class WaveTypeExtensions
    {
        public static LLVMTypeRef AsLLVM(this WaveType type)
        {
            if (type.IsPrimitive)
                return type.TypeCode.AsLLVM();
            return default;
        }
        public static LLVMTypeRef AsLLVM(this WaveTypeCode type)
        {
            switch (type)
            {
                case WaveTypeCode.TYPE_VOID:
                    return LLVM.VoidType();
                case WaveTypeCode.TYPE_OBJECT:
                    throw new NotImplementedException();
                case WaveTypeCode.TYPE_BOOLEAN:
                    return LLVM.Int1Type();
                case WaveTypeCode.TYPE_CHAR:
                    throw new NotImplementedException();
                case WaveTypeCode.TYPE_I1:
                    return LLVM.Int8Type();
                case WaveTypeCode.TYPE_U1:
                    return LLVM.Int8Type();
                case WaveTypeCode.TYPE_I2:
                    return LLVM.Int16Type();
                case WaveTypeCode.TYPE_U2:
                    throw new NotImplementedException();
                case WaveTypeCode.TYPE_I4:
                    return LLVM.Int32Type();
                case WaveTypeCode.TYPE_U4:
                    throw new NotImplementedException();
                case WaveTypeCode.TYPE_I8:
                    return LLVM.Int64Type();
                case WaveTypeCode.TYPE_U8:
                    throw new NotImplementedException();
                case WaveTypeCode.TYPE_R2:
                    return LLVM.HalfType();
                case WaveTypeCode.TYPE_R4:
                    return LLVM.FloatType();
                case WaveTypeCode.TYPE_R8:
                    return LLVM.DoubleType();
                case WaveTypeCode.TYPE_R16:
                    return LLVM.FP128Type();
                case WaveTypeCode.TYPE_STRING:
                    throw new NotImplementedException();
            }
            throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }
    }
}