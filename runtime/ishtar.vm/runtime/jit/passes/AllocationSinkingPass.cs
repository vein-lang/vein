namespace ishtar.jit;

/// <summary>
/// Allocation Sinking: moves object allocations closer to their actual use,
/// or eliminates them if the object doesn't escape the method.
///
/// Strategy:
/// 1. Find NewObj/NewArr/Alloc instructions
/// 2. If the allocation's result only flows to local stores/loads and doesn't escape
///    (not passed to calls, not stored to fields, not returned), mark it for scalar replacement
/// 3. If it does escape but is only used in one branch, sink it to that branch
/// </summary>
public static unsafe class AllocationSinkingPass
{

    public static bool Run(IRFunction* fn)
    {
        var changed = false;

        for (var i = 0; i < fn->InstructionCount; i++)
        {
            var instr = &fn->Instructions[i];
            if (instr->IsDead) continue;
            if (instr->Op is not (IROp.NewObj or IROp.NewArr or IROp.Alloc)) continue;
            if (instr->ResultId < 0) continue;

            var allocValId = instr->ResultId;

            // Check if the allocation escapes
            if (!DoesEscape(fn, allocValId))
            {
                // Non-escaping allocation: mark as dead (scalar replacement opportunity)
                // Replace all loads from this object with their stored values
                // For now, just eliminate allocations with zero uses after DCE
                if (CountUses(fn, allocValId) == 0)
                {
                    instr->IsDead = true;
                    changed = true;
                }
            }
            else
            {
                // Try to sink: if used only in one successor path, move there
                if (TrySinkToUseBlock(fn, instr, allocValId))
                    changed = true;
            }
        }

        return changed;
    }

    private static bool DoesEscape(IRFunction* fn, int valueId)
    {
        for (var i = 0; i < fn->InstructionCount; i++)
        {
            var instr = &fn->Instructions[i];
            if (instr->IsDead) continue;

            for (var j = 0; j < instr->OperandCount; j++)
            {
                if (instr->GetOperand(j) != valueId) continue;

                // Escaping uses: calls, stores to heap, returns
                switch (instr->Op)
                {
                    case IROp.Call:
                    case IROp.CallVirt:
                    case IROp.CallIndirect:
                    case IROp.StoreField:
                    case IROp.StoreElem:
                    case IROp.Return:
                        return true;
                }
            }
        }
        return false;
    }

    private static int CountUses(IRFunction* fn, int valueId)
    {
        var count = 0;
        for (var i = 0; i < fn->InstructionCount; i++)
        {
            var instr = &fn->Instructions[i];
            if (instr->IsDead) continue;
            for (var j = 0; j < instr->OperandCount; j++)
            {
                if (instr->GetOperand(j) == valueId)
                    count++;
            }
        }
        return count;
    }

    private static bool TrySinkToUseBlock(IRFunction* fn, IRInstruction* allocInstr, int allocValId)
    {
        // Find all blocks that use this value
        var allocBlock = allocInstr->BlockIndex;
        var useBlock = -1;
        var multiBlock = false;

        for (var i = 0; i < fn->InstructionCount; i++)
        {
            var instr = &fn->Instructions[i];
            if (instr->IsDead) continue;
            if (instr == allocInstr) continue;

            for (var j = 0; j < instr->OperandCount; j++)
            {
                if (instr->GetOperand(j) != allocValId) continue;

                if (useBlock == -1)
                    useBlock = instr->BlockIndex;
                else if (instr->BlockIndex != useBlock)
                    multiBlock = true;
                break;
            }

            if (multiBlock) break;
        }

        // Only sink if all uses are in a single different block
        if (multiBlock || useBlock == -1 || useBlock == allocBlock)
            return false;

        // Move the instruction to the beginning of the use block
        allocInstr->BlockIndex = useBlock;
        return true;
    }
}
