namespace ishtar.jit;

/// <summary>
/// Bounds Check Elimination: removes array bounds checks that are provably safe.
/// Uses range information (from RangeInferencingPass) to determine if an index
/// is always within [0, array.Length).
/// </summary>
public static unsafe class BoundsCheckEliminationPass
{

    public static bool Run(IRFunction* fn)
    {
        var changed = false;

        for (var i = 0; i < fn->InstructionCount; i++)
        {
            var instr = &fn->Instructions[i];
            if (instr->IsDead) continue;
            if (instr->Op != IROp.BoundsCheck) continue;

            // BoundsCheck(index, arrayLen)
            var indexId = instr->GetOperand(0);
            var lenId = instr->GetOperand(1);

            if (CanEliminateCheck(fn, indexId, lenId))
            {
                instr->IsDead = true;
                changed = true;
            }
        }

        return changed;
    }

    private static bool CanEliminateCheck(IRFunction* fn, int indexId, int lenId)
    {
        // Case 1: index is a constant and len is a constant
        var indexDef = ConstantFoldingPass.FindConstDef(fn, indexId);
        var lenDef = ConstantFoldingPass.FindConstDef(fn, lenId);

        if (indexDef != null && lenDef != null)
        {
            var idx = indexDef->Immediate;
            var len = lenDef->Immediate;
            return idx >= 0 && idx < len;
        }

        // Case 2: index is a constant in [0, ...) and we know len > index
        if (indexDef != null && indexDef->Immediate < 0)
            return false; // negative constant — cannot eliminate

        // Case 3: check if index has range metadata attached
        // Range info is stored as: Values[id].DefInstrIndex points to an instruction
        // that was annotated by RangeInferencingPass with Immediate2 encoding [lo, hi]
        // (packed as two int32s in a long)
        if (indexId >= 0 && indexId < fn->ValueCount)
        {
            var indexVal = &fn->Values[indexId];
            if (indexVal->DefInstrIndex >= 0)
            {
                var def = &fn->Instructions[indexVal->DefInstrIndex];
                // Check if range annotation exists (stored in Immediate2 by range pass)
                var rangePacked = def->Immediate2;
                if (rangePacked != 0) // 0 means no range info
                {
                    var lo = (int)(rangePacked & 0xFFFFFFFF);
                    var hi = (int)((rangePacked >> 32) & 0xFFFFFFFF);

                    if (lo >= 0)
                    {
                        // If len is constant and hi < len, safe to eliminate
                        if (lenDef != null && hi < lenDef->Immediate)
                            return true;
                    }
                }
            }
        }

        return false;
    }
}
