namespace ishtar.jit;

/// <summary>
/// Code Motion (Loop-Invariant Code Motion / LICM):
/// Moves loop-invariant computations out of loops into the preheader.
/// An instruction is loop-invariant if all its operands are defined outside the loop
/// or are themselves loop-invariant.
/// </summary>
public static unsafe class CodeMotionPass
{

    public static bool Run(IRFunction* fn)
    {
        var changed = false;
        var loopCount = fn->Loops->Count;

        for (var li = 0; li < loopCount; li++)
        {
            var loop = fn->Loops->Get(li);
            if (HoistInvariants(fn, loop))
                changed = true;
        }

        return changed;
    }

    private static bool HoistInvariants(IRFunction* fn, IRLoop* loop)
    {
        var changed = false;
        var headerIdx = loop->HeaderBlockIndex;
        var header = &fn->Blocks[headerIdx];

        // The preheader is a predecessor of the header that is NOT in the loop body
        var preheaderIdx = FindPreheader(fn, loop);
        if (preheaderIdx < 0) return false;

        var bodyCount = loop->BodyBlocks->Count;

        // Iterate over all instructions in loop body blocks
        for (var bi = 0; bi < bodyCount; bi++)
        {
            var blockIdx = loop->BodyBlocks->Get(bi)->Id;
            var block = &fn->Blocks[blockIdx];
            var instrCount = block->Instructions->Count;

            for (var i = 0; i < instrCount; i++)
            {
                var instrId = block->Instructions->Get(i)->Id;
                var instr = &fn->Instructions[instrId];
                if (instr->IsDead) continue;
                if (instr->ResultId < 0) continue;
                if (!IsPureMoveable(instr->Op)) continue;

                if (IsLoopInvariant(fn, instr, loop))
                {
                    // Move instruction to preheader
                    instr->BlockIndex = preheaderIdx;
                    fn->AppendToBlock(preheaderIdx, instrId);
                    // Note: we don't remove from original block list here;
                    // DCE will handle the dead slot or a compaction pass can clean up
                    changed = true;
                }
            }
        }

        return changed;
    }

    private static bool IsLoopInvariant(IRFunction* fn, IRInstruction* instr, IRLoop* loop)
    {
        for (var i = 0; i < instr->OperandCount; i++)
        {
            var opId = instr->GetOperand(i);
            if (opId < 0 || opId >= fn->ValueCount) continue;

            var opVal = &fn->Values[opId];
            var defBlock = opVal->DefBlockIndex;

            // If defined inside the loop, not invariant (unless that def is also invariant — 
            // we handle this conservatively: only hoist if all operands are defined outside)
            if (IsInLoop(loop, defBlock))
                return false;
        }
        return true;
    }

    private static bool IsInLoop(IRLoop* loop, int blockIdx)
    {
        var bodyCount = loop->BodyBlocks->Count;
        for (var i = 0; i < bodyCount; i++)
        {
            if (loop->BodyBlocks->Get(i)->Id == blockIdx)
                return true;
        }
        return false;
    }

    private static int FindPreheader(IRFunction* fn, IRLoop* loop)
    {
        var headerIdx = loop->HeaderBlockIndex;
        var header = &fn->Blocks[headerIdx];
        var predCount = header->Predecessors->Count;

        for (var i = 0; i < predCount; i++)
        {
            var predIdx = header->Predecessors->Get(i)->Id;
            if (!IsInLoop(loop, predIdx))
                return predIdx;
        }

        return -1;
    }

    private static bool IsPureMoveable(IROp op) => op is
        IROp.Const or IROp.Add or IROp.Sub or IROp.Mul or IROp.Div or IROp.Mod or
        IROp.And or IROp.Or or IROp.Xor or IROp.Shl or IROp.Shr or
        IROp.Neg or IROp.Not or
        IROp.CmpEq or IROp.CmpNe or IROp.CmpLt or IROp.CmpLe or IROp.CmpGt or IROp.CmpGe or
        IROp.Conv or IROp.ArrayLen;
}
