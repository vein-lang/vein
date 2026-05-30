namespace ishtar.jit;

/// <summary>
/// Constant Folding: evaluates operations on constant operands at compile time.
/// e.g., ADD(Const(3), Const(4)) → Const(7)
/// </summary>
public static unsafe class ConstantFoldingPass
{

    public static bool Run(IRFunction* fn)
    {
        var changed = false;

        for (var i = 0; i < fn->InstructionCount; i++)
        {
            var instr = &fn->Instructions[i];
            if (instr->IsDead) continue;
            if (instr->ResultId < 0) continue;

            switch (instr->Op)
            {
                case IROp.Add:
                case IROp.Sub:
                case IROp.Mul:
                case IROp.Div:
                case IROp.Mod:
                case IROp.And:
                case IROp.Or:
                case IROp.Xor:
                case IROp.Shl:
                case IROp.Shr:
                    if (TryFoldBinary(fn, instr))
                        changed = true;
                    break;
                case IROp.Neg:
                case IROp.Not:
                    if (TryFoldUnary(fn, instr))
                        changed = true;
                    break;
                case IROp.CmpEq:
                case IROp.CmpNe:
                case IROp.CmpLt:
                case IROp.CmpLe:
                case IROp.CmpGt:
                case IROp.CmpGe:
                    if (TryFoldCompare(fn, instr))
                        changed = true;
                    break;
            }
        }

        return changed;
    }

    private static bool TryFoldBinary(IRFunction* fn, IRInstruction* instr)
    {
        var lhsId = instr->GetOperand(0);
        var rhsId = instr->GetOperand(1);

        var lhsDef = FindConstDef(fn, lhsId);
        var rhsDef = FindConstDef(fn, rhsId);

        if (lhsDef == null || rhsDef == null) return false;

        var lv = lhsDef->Immediate;
        var rv = rhsDef->Immediate;
        var resultType = fn->Values[instr->ResultId].Type;

        long result;
        switch (instr->Op)
        {
            case IROp.Add: result = lv + rv; break;
            case IROp.Sub: result = lv - rv; break;
            case IROp.Mul: result = lv * rv; break;
            case IROp.Div: result = rv != 0 ? lv / rv : 0; break;
            case IROp.Mod: result = rv != 0 ? lv % rv : 0; break;
            case IROp.And: result = lv & rv; break;
            case IROp.Or: result = lv | rv; break;
            case IROp.Xor: result = lv ^ rv; break;
            case IROp.Shl: result = lv << (int)rv; break;
            case IROp.Shr: result = lv >> (int)rv; break;
            default: return false;
        }

        // Replace instruction with a Const
        instr->Op = IROp.Const;
        instr->Immediate = result;
        instr->OperandCount = 0;
        instr->TargetType = resultType;
        return true;
    }

    private static bool TryFoldUnary(IRFunction* fn, IRInstruction* instr)
    {
        var opId = instr->GetOperand(0);
        var opDef = FindConstDef(fn, opId);
        if (opDef == null) return false;

        var v = opDef->Immediate;
        var resultType = fn->Values[instr->ResultId].Type;

        long result = instr->Op switch
        {
            IROp.Neg => -v,
            IROp.Not => ~v,
            _ => 0
        };

        instr->Op = IROp.Const;
        instr->Immediate = result;
        instr->OperandCount = 0;
        instr->TargetType = resultType;
        return true;
    }

    private static bool TryFoldCompare(IRFunction* fn, IRInstruction* instr)
    {
        var lhsId = instr->GetOperand(0);
        var rhsId = instr->GetOperand(1);

        var lhsDef = FindConstDef(fn, lhsId);
        var rhsDef = FindConstDef(fn, rhsId);

        if (lhsDef == null || rhsDef == null) return false;

        var lv = lhsDef->Immediate;
        var rv = rhsDef->Immediate;

        long result = instr->Op switch
        {
            IROp.CmpEq => lv == rv ? 1 : 0,
            IROp.CmpNe => lv != rv ? 1 : 0,
            IROp.CmpLt => lv < rv ? 1 : 0,
            IROp.CmpLe => lv <= rv ? 1 : 0,
            IROp.CmpGt => lv > rv ? 1 : 0,
            IROp.CmpGe => lv >= rv ? 1 : 0,
            _ => 0
        };

        instr->Op = IROp.Const;
        instr->Immediate = result;
        instr->OperandCount = 0;
        instr->TargetType = IRType.Bool;
        return true;
    }

    internal static IRInstruction* FindConstDef(IRFunction* fn, int valueId)
    {
        if (valueId < 0 || valueId >= fn->ValueCount) return null;
        var val = &fn->Values[valueId];
        if (val->DefInstrIndex < 0) return null;
        var def = &fn->Instructions[val->DefInstrIndex];
        if (def->Op == IROp.Const && !def->IsDead)
            return def;
        return null;
    }
}
