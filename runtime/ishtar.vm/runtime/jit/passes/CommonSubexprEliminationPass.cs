namespace ishtar.jit;

/// <summary>
/// Common Subexpression Elimination: if two instructions compute the same value
/// (same op, same operands, same immediates), eliminate the second and reuse the first's result.
/// Only applies to pure (no side-effect) instructions.
/// </summary>
public static unsafe class CommonSubexprEliminationPass
{

    public static bool Run(IRFunction* fn)
    {
        var changed = false;

        // Process each block independently (local CSE)
        for (var b = 0; b < fn->BlockCount; b++)
        {
            var block = &fn->Blocks[b];
            var instrCount = block->Instructions->Count;

            for (var i = 0; i < instrCount; i++)
            {
                var instrId = block->Instructions->Get(i)->Id;
                var instr = &fn->Instructions[instrId];
                if (instr->IsDead) continue;
                if (instr->ResultId < 0) continue;
                if (!IsPure(instr->Op)) continue;

                // Look for a dominating instruction in this block with same expression
                for (var j = 0; j < i; j++)
                {
                    var prevId = block->Instructions->Get(j)->Id;
                    var prev = &fn->Instructions[prevId];
                    if (prev->IsDead) continue;
                    if (prev->ResultId < 0) continue;

                    if (SameExpression(instr, prev))
                    {
                        // Replace uses of instr's result with prev's result
                        fn->ReplaceAllUses(instr->ResultId, prev->ResultId);
                        instr->IsDead = true;
                        changed = true;
                        break;
                    }
                }
            }
        }

        return changed;
    }

    private static bool SameExpression(IRInstruction* a, IRInstruction* b)
    {
        if (a->Op != b->Op) return false;
        if (a->OperandCount != b->OperandCount) return false;
        if (a->Immediate != b->Immediate) return false;
        if (a->Immediate2 != b->Immediate2) return false;
        if (a->TargetType != b->TargetType) return false;

        for (var i = 0; i < a->OperandCount; i++)
        {
            if (a->GetOperand(i) != b->GetOperand(i))
                return false;
        }

        return true;
    }

    private static bool IsPure(IROp op) => op is
        IROp.Const or IROp.Add or IROp.Sub or IROp.Mul or IROp.Div or IROp.Mod or
        IROp.And or IROp.Or or IROp.Xor or IROp.Shl or IROp.Shr or
        IROp.Neg or IROp.Not or
        IROp.CmpEq or IROp.CmpNe or IROp.CmpLt or IROp.CmpLe or IROp.CmpGt or IROp.CmpGe or
        IROp.Conv or IROp.ArrayLen or IROp.LoadArg or IROp.LoadLocal;
}
