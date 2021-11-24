namespace ishtar.jit.registers;

public enum GP_REGISTER_TYPE
{
    GpbLo = 0b1,
    GpbHi = 0b10,
    Gpw = 0b10000,
    Gpd = 0b100000,
    Gpq = 0b110000,
}
