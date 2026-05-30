namespace ishtar.jit;

/// <summary>
/// Range Inferencing: propagates integer range information [lo, hi] through the IR.
/// Used by BoundsCheckElimination to prove indices are in-bounds.
///
/// Strategy:
/// - Constants: range is [value, value]
/// - Add(a, b): [a.lo + b.lo, a.hi + b.hi]
/// - LoadArg with known non-negative (array index patterns): [0, INT_MAX]
/// - Phi: union of incoming ranges
/// - Induction variables in loops: [init, init + step * (tripCount - 1)]
///
/// Range is packed into Immediate2 of the defining instruction as two int32s:
///   lo = (int)(Immediate2 & 0xFFFFFFFF)
///   hi = (int)(Immediate2 >> 32)
/// A value of 0 in Immediate2 means "no range info" (ambiguous but acceptable
/// since [0,0] is a valid range — use a sentinel bit pattern if needed later).
/// </summary>
public static unsafe class RangeInferencingPass
{

    private const int RangeUnknownLo = int.MinValue;
    private const int RangeUnknownHi = int.MaxValue;

    public static bool Run(IRFunction* fn)
    {
        var changed = false;

        for (var i = 0; i < fn->InstructionCount; i++)
        {
            var instr = &fn->Instructions[i];
            if (instr->IsDead) continue;
            if (instr->ResultId < 0) continue;

            var resultType = fn->Values[instr->ResultId].Type;
            if (!IRTypeMap.IsInteger(resultType)) continue;

            long newRange;
            switch (instr->Op)
            {
                case IROp.Const:
                    newRange = PackRange((int)instr->Immediate, (int)instr->Immediate);
                    break;

                case IROp.Add:
                    newRange = InferBinary(fn, instr, static (lo1, hi1, lo2, hi2) =>
                        PackRange(SatAdd(lo1, lo2), SatAdd(hi1, hi2)));
                    break;

                case IROp.Sub:
                    newRange = InferBinary(fn, instr, static (lo1, hi1, lo2, hi2) =>
                        PackRange(SatSub(lo1, hi2), SatSub(hi1, lo2)));
                    break;

                case IROp.Mul:
                    newRange = InferBinary(fn, instr, static (lo1, hi1, lo2, hi2) =>
                    {
                        // Conservative: take min/max of all corner products
                        var a = (long)lo1 * lo2;
                        var b = (long)lo1 * hi2;
                        var c = (long)hi1 * lo2;
                        var d = (long)hi1 * hi2;
                        var min = Math.Min(Math.Min(a, b), Math.Min(c, d));
                        var max = Math.Max(Math.Max(a, b), Math.Max(c, d));
                        return PackRange(Clamp(min), Clamp(max));
                    });
                    break;

                case IROp.And:
                    // AND with non-negative = result is non-negative and ≤ smaller operand
                    newRange = InferAnd(fn, instr);
                    break;

                case IROp.LoadArg:
                case IROp.LoadLocal:
                    // Unknown range
                    newRange = PackRange(RangeUnknownLo, RangeUnknownHi);
                    break;

                default:
                    newRange = PackRange(RangeUnknownLo, RangeUnknownHi);
                    break;
            }

            if (newRange != 0 && instr->Immediate2 != newRange)
            {
                instr->Immediate2 = newRange;
                changed = true;
            }
        }

        return changed;
    }

    private static long InferBinary(IRFunction* fn, IRInstruction* instr,
        Func<int, int, int, int, long> combine)
    {
        var lhsRange = GetRange(fn, instr->GetOperand(0));
        var rhsRange = GetRange(fn, instr->GetOperand(1));

        var (lo1, hi1) = UnpackRange(lhsRange);
        var (lo2, hi2) = UnpackRange(rhsRange);

        return combine(lo1, hi1, lo2, hi2);
    }

    private static long InferAnd(IRFunction* fn, IRInstruction* instr)
    {
        var lhsRange = GetRange(fn, instr->GetOperand(0));
        var rhsRange = GetRange(fn, instr->GetOperand(1));

        var (_, hi1) = UnpackRange(lhsRange);
        var (_, hi2) = UnpackRange(rhsRange);

        // AND: result ≤ min(hi1, hi2) and ≥ 0 if either operand is non-negative
        var (lo1, _) = UnpackRange(lhsRange);
        var (lo2, _2) = UnpackRange(rhsRange);

        var resLo = (lo1 >= 0 || lo2 >= 0) ? 0 : RangeUnknownLo;
        var resHi = Math.Min(Math.Abs(hi1), Math.Abs(hi2));

        return PackRange(resLo, resHi);
    }

    private static long GetRange(IRFunction* fn, int valueId)
    {
        if (valueId < 0 || valueId >= fn->ValueCount) return PackRange(RangeUnknownLo, RangeUnknownHi);
        var val = &fn->Values[valueId];
        if (val->DefInstrIndex < 0) return PackRange(RangeUnknownLo, RangeUnknownHi);
        var def = &fn->Instructions[val->DefInstrIndex];
        return def->Immediate2 != 0 ? def->Immediate2 : PackRange(RangeUnknownLo, RangeUnknownHi);
    }

    internal static long PackRange(int lo, int hi)
    {
        return (long)(uint)lo | ((long)(uint)hi << 32);
    }

    internal static (int lo, int hi) UnpackRange(long packed)
    {
        var lo = (int)(packed & 0xFFFFFFFF);
        var hi = (int)((packed >> 32) & 0xFFFFFFFF);
        return (lo, hi);
    }

    private static int SatAdd(int a, int b)
    {
        var r = (long)a + b;
        return Clamp(r);
    }

    private static int SatSub(int a, int b)
    {
        var r = (long)a - b;
        return Clamp(r);
    }

    private static int Clamp(long v) => v > int.MaxValue ? int.MaxValue : v < int.MinValue ? int.MinValue : (int)v;
}
