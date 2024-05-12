namespace ishtar.runtime;

using collections;
using emit;

public readonly unsafe struct ProtectedZone(
    uint startAddr,
    uint endAddr,
    int tryEndLabel,
    NativeList<int> filterAddr,
    NativeList<int> catchAddr,
    DirectNativeList<RuntimeQualityTypeName>* catchClass,
    NativeList<ExceptionMarkKind> types)
{
    public uint StartAddr { get; } = startAddr;
    public uint EndAddr { get; } = endAddr;
    public int TryEndLabel { get; } = tryEndLabel;
    public NativeList<int> FilterAddr { get; } = filterAddr;
    public NativeList<int> CatchAddr { get; } = catchAddr;
    public DirectNativeList<RuntimeQualityTypeName>* CatchClass { get; } = catchClass;
    public NativeList<ExceptionMarkKind> Types { get; } = types;
}
