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
    AtomicNativeList<byte>* types) : IEq<ProtectedZone>
{
    public uint StartAddr { get; } = startAddr;
    public uint EndAddr { get; } = endAddr;
    public int TryEndLabel { get; } = tryEndLabel;
    public AtomicNativeList<int>* FilterAddr { get; } = filterAddr;
    public AtomicNativeList<int>* CatchAddr { get; } = catchAddr;
    public NativeList<RuntimeQualityTypeName>* CatchClass { get; } = catchClass;
    public AtomicNativeList<byte>* Types { get; } = types;

    public static bool Eq(ProtectedZone* p1, ProtectedZone* p2) =>
        p1->StartAddr == p2->StartAddr && p1->EndAddr == p2->EndAddr && p1->TryEndLabel == p2->TryEndLabel
        && p1->CatchClass->Count == p2->CatchClass->Count
        && p1->FilterAddr->Count == p2->FilterAddr->Count
        && p1->Types->Count == p2->Types->Count;
}
