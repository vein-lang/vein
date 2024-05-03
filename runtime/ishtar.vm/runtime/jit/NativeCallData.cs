namespace ishtar;

public struct NativeCallData
{
    public nint procedure;
    public long argCount;
    public nint returnMemoryPointer;
    public nint argsPointer;
}