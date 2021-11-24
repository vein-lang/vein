namespace ishtar.jit.registers;

internal enum REGISTER_TYPE
{
    INVALID = 0b0,
    GpbLo = GP_REGISTER_TYPE.GpbLo,
    GpbHi = GP_REGISTER_TYPE.GpbHi,
    PatchedGpbHi = GpbLo | GpbHi,
    GPW = GP_REGISTER_TYPE.Gpw,
    GPD = GP_REGISTER_TYPE.Gpd,
    GPQ = GP_REGISTER_TYPE.Gpq,
    FP = 0b1000000,
    MM = 0b1010000,
    K = 0b1100000,
    XMM = 0b1110000,
    YMM = 0b10000000,
    ZMM = 0b10010000,
    RIP = 0b11100000,
    SEG = 0b11110000
}
