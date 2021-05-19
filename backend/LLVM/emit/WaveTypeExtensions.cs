namespace wave.llvm.emit
{
    using System;
    using LLVMSharp;
    using runtime;

    public static class ManaTypeExtensions
    {
        public static LLVMTypeRef AsLLVM(this ManaType type)
        {
            if (type.IsPrimitive)
                return type.TypeCode.AsLLVM();
            return default;
        }
        public static LLVMTypeRef AsLLVM(this ManaTypeCode type)
        {
            switch (type)
            {
                case ManaTypeCode.TYPE_VOID:
                    return LLVM.VoidType();
                case ManaTypeCode.TYPE_OBJECT:
                    throw new NotImplementedException();
                case ManaTypeCode.TYPE_BOOLEAN:
                    return LLVM.Int1Type();
                case ManaTypeCode.TYPE_CHAR:
                    throw new NotImplementedException();
                case ManaTypeCode.TYPE_I1:
                    return LLVM.Int8Type();
                case ManaTypeCode.TYPE_U1:
                    return LLVM.Int8Type();
                case ManaTypeCode.TYPE_I2:
                    return LLVM.Int16Type();
                case ManaTypeCode.TYPE_U2:
                    throw new NotImplementedException();
                case ManaTypeCode.TYPE_I4:
                    return LLVM.Int32Type();
                case ManaTypeCode.TYPE_U4:
                    throw new NotImplementedException();
                case ManaTypeCode.TYPE_I8:
                    return LLVM.Int64Type();
                case ManaTypeCode.TYPE_U8:
                    throw new NotImplementedException();
                case ManaTypeCode.TYPE_R2:
                    return LLVM.HalfType();
                case ManaTypeCode.TYPE_R4:
                    return LLVM.FloatType();
                case ManaTypeCode.TYPE_R8:
                    return LLVM.DoubleType();
                case ManaTypeCode.TYPE_R16:
                    return LLVM.FP128Type();
                case ManaTypeCode.TYPE_STRING:
                    throw new NotImplementedException();
            }
            throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }
    }
}