namespace ishtar.jit;

/// <summary>
/// Dead Code Elimination: removes instructions whose results are never used.
/// </summary>
public static unsafe class DeadCodeEliminationPass
{

    public static bool Run(IRFunction* fn)
    {
        // Build use-count array
        var useCounts = stackalloc int[fn->ValueCount];
        for (var i = 0; i < fn->ValueCount; i++)
            useCounts[i] = 0;

        for (var i = 0; i < fn->InstructionCount; i++)
        {
            var instr = &fn->Instructions[i];
            if (instr->IsDead) continue;
            for (var j = 0; j < instr->OperandCount; j++)
            {
                var opId = instr->GetOperand(j);
                if (opId >= 0 && opId < fn->ValueCount)
                    useCounts[opId]++;
            }
        }

        var changed = false;

        for (var i = 0; i < fn->InstructionCount; i++)
        {
            var instr = &fn->Instructions[i];
            if (instr->IsDead) continue;
            if (instr->ResultId < 0) continue;

            // Don't eliminate side-effectful operations
            if (HasSideEffects(instr->Op)) continue;

            // If result is unused, mark dead
            if (useCounts[instr->ResultId] == 0)
            {
                instr->IsDead = true;
                changed = true;
            }
        }

        return changed;
    }

    private static bool HasSideEffects(IROp op) => op is
        IROp.Store or IROp.StoreField or IROp.StoreElem or IROp.StoreArg or IROp.StoreLocal or
        IROp.Call or IROp.CallVirt or IROp.CallIndirect or
        IROp.NewObj or IROp.NewArr or IROp.Alloc or
        IROp.Branch or IROp.BranchTrue or IROp.BranchFalse or IROp.Return or
        IROp.BoundsCheck;
}
