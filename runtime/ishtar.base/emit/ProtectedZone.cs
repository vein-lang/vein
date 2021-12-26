namespace ishtar.emit;

using vein.runtime;

public readonly record struct ProtectedZone(
    uint startAddr,
    uint endAddr,
    int tryEndLabel,
    int[] filterAddr,
    int[] catchAddr,
    QualityTypeName[] catchClass,
    ExceptionMarkKind[] types);
