namespace ishtar.jit;

/// <summary>
/// Loop Unrolling: for small loops with known constant trip counts, duplicates the loop body
/// to reduce branch overhead and enable further optimizations.
/// Only unrolls loops with trip count ≤ MaxUnrollCount and body size ≤ MaxBodySize.
/// </summary>
public static unsafe class LoopUnrollingPass
{

    private const int MaxUnrollCount = 8;
    private const int MaxBodySize = 16; // max instructions in loop body to consider unrolling

    public static bool Run(IRFunction* fn)
    {
        var changed = false;
        var loopCount = fn->Loops->Count;

        for (var li = 0; li < loopCount; li++)
        {
            var loop = fn->Loops->Get(li);

            if (loop->TripCount <= 0 || loop->TripCount > MaxUnrollCount)
                continue;

            // Check body size
            var bodySize = CountBodyInstructions(fn, loop);
            if (bodySize > MaxBodySize)
                continue;

            if (UnrollLoop(fn, loop))
                changed = true;
        }

        return changed;
    }

    private static int CountBodyInstructions(IRFunction* fn, IRLoop* loop)
    {
        var count = 0;
        var bodyCount = loop->BodyBlocks->Count;
        for (var i = 0; i < bodyCount; i++)
        {
            var blockIdx = loop->BodyBlocks->Get(i)->Id;
            count += fn->Blocks[blockIdx].Instructions->Count;
        }
        return count;
    }

    private static bool UnrollLoop(IRFunction* fn, IRLoop* loop)
    {
        var tripCount = (int)loop->TripCount;
        var headerIdx = loop->HeaderBlockIndex;
        var header = &fn->Blocks[headerIdx];

        // Collect body block indices (excluding header's branch back-edge logic)
        var bodyCount = loop->BodyBlocks->Count;
        if (bodyCount == 0) return false;

        // Simple case: single-block loop (header is the body)
        // We duplicate all non-terminator instructions (tripCount - 1) times,
        // then replace the back-edge with a fall-through.
        if (bodyCount == 1 && loop->BodyBlocks->Get(0)->Id == headerIdx)
        {
            var instrCount = header->Instructions->Count;
            if (instrCount <= 1) return false; // only terminator

            // Collect non-terminator instruction IDs
            var bodyInstrCount = instrCount - 1; // exclude terminator

            // Duplicate body instructions (tripCount - 1) more times
            for (var iter = 1; iter < tripCount; iter++)
            {
                for (var i = 0; i < bodyInstrCount; i++)
                {
                    var origId = header->Instructions->Get(i)->Id;
                    var orig = &fn->Instructions[origId];
                    if (orig->IsDead) continue;

                    // Clone instruction with new value IDs
                    var newInstr = *orig;
                    newInstr.IsDead = false;

                    if (newInstr.ResultId >= 0)
                    {
                        var oldType = fn->Values[newInstr.ResultId].Type;
                        var newValId = fn->AllocValue(oldType, headerIdx, fn->InstructionCount);
                        newInstr.ResultId = newValId;
                    }

                    var newId = fn->AddInstruction(newInstr);
                    fn->AppendToBlock(headerIdx, newId);
                }
            }

            // Replace back-edge terminator with unconditional branch to exit
            var termId = header->Instructions->Get(header->Instructions->Count - 1)->Id;
            var term = &fn->Instructions[termId];

            if (term->Op is IROp.BranchTrue or IROp.BranchFalse)
            {
                // The exit is the non-header target
                var exitBlock = term->BranchTarget0 == headerIdx ? term->BranchTarget1 : term->BranchTarget0;
                term->Op = IROp.Branch;
                term->BranchTarget0 = exitBlock;
                term->BranchTarget1 = -1;
                term->OperandCount = 0;
            }

            return true;
        }

        // Multi-block loop unrolling: for now, skip (future work)
        return false;
    }
}
