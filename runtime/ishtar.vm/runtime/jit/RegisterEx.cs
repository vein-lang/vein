namespace ishtar;

using Iced.Intel;

public static class RegisterEx
{
    public static RegisterKind GetKind(this Register reg)
    {
        if (reg.IsGPR64())
            return RegisterKind.GPR64;
        if (reg.IsGPR32())
            return RegisterKind.GPR32;
        return RegisterKind.GPR8;
    }
}