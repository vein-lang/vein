namespace ishtar.runtime;

using collections;
using emit;

public readonly unsafe struct ProtectedZone(
    uint startAddr,
    uint endAddr,
    int tryEndLabel,
    AtomicNativeList<int>* filterAddr,
    AtomicNativeList<int>* catchAddr,
    NativeList<RuntimeQualityTypeName>* catchClass,
    AtomicNativeList<byte>* types)
{
    public uint StartAddr { get; } = startAddr;
    public uint EndAddr { get; } = endAddr;
    public int TryEndLabel { get; } = tryEndLabel;
    public AtomicNativeList<int>* FilterAddr { get; } = filterAddr;
    public AtomicNativeList<int>* CatchAddr { get; } = catchAddr;
    public NativeList<RuntimeQualityTypeName>* CatchClass { get; } = catchClass;
    public AtomicNativeList<byte>* Types { get; } = types;
}
