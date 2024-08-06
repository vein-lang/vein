namespace ishtar;


[CTypeExport("ishtar_ncd_t")]
public struct NativeCallData
{
    public nint procedure;
    public long argCount;
    public nint returnMemoryPointer;
    public nint argsPointer;
}
