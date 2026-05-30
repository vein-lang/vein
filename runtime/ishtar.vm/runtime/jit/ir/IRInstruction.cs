namespace ishtar.jit;

using ishtar.collections;

/// <summary>
/// A single SSA instruction in the IR. Unmanaged struct — lives in native memory.
/// Each instruction has an opcode, produces at most one value (ResultId),
/// and consumes up to 4 operand values inline (overflow via OperandsExtra pointer).
/// </summary>
public unsafe struct IRInstruction : IEq<IRInstruction>, IEquatable<IRInstruction>
{
    public int Id;
    public IROp Op;

    /// <summary>Value ID produced by this instruction (-1 for void ops).</summary>
    public int ResultId;

    /// <summary>Inline operand value IDs (up to 4). -1 = unused slot.</summary>
    public fixed int Operands[4];

    /// <summary>Number of operands actually used (including overflow).</summary>
    public byte OperandCount;

    /// <summary>Pointer to additional operand IDs when > 4 (allocated from same arena).</summary>
    public int* OperandsExtra;

    /// <summary>Immediate constant value (for Const, LoadArg index, etc.).</summary>
    public long Immediate;

    /// <summary>Secondary immediate (field token, type token).</summary>
    public long Immediate2;

    /// <summary>Target type for Conv, Cast, NewObj, NewArr.</summary>
    public IRType TargetType;

    /// <summary>Branch target block indices. -1 = unused.</summary>
    public int BranchTarget0;
    public int BranchTarget1;

    /// <summary>Method/function pointer for Call instructions.</summary>
    public nint MethodRef;

    /// <summary>Whether this instruction has been marked dead.</summary>
    public bool IsDead;

    /// <summary>Block index this instruction belongs to.</summary>
    public int BlockIndex;

    public int GetOperand(int index)
    {
        if (index < 4) return Operands[index];
        return OperandsExtra[index - 4];
    }

    public void SetOperand(int index, int valueId)
    {
        if (index < 4) Operands[index] = valueId;
        else OperandsExtra[index - 4] = valueId;
    }

    public static bool Eq(IRInstruction* p1, IRInstruction* p2) => p1->Id == p2->Id;
    public bool Equals(IRInstruction other) => Id == other.Id;

    public static IRInstruction CreateConst(int id, int resultId, long value, IRType type)
    {
        var instr = new IRInstruction();
        instr.Id = id;
        instr.Op = IROp.Const;
        instr.ResultId = resultId;
        instr.Immediate = value;
        instr.TargetType = type;
        instr.OperandCount = 0;
        instr.BranchTarget0 = -1;
        instr.BranchTarget1 = -1;
        return instr;
    }

    public static IRInstruction CreateBinary(int id, IROp op, int resultId, int lhs, int rhs)
    {
        var instr = new IRInstruction();
        instr.Id = id;
        instr.Op = op;
        instr.ResultId = resultId;
        instr.OperandCount = 2;
        instr.Operands[0] = lhs;
        instr.Operands[1] = rhs;
        instr.BranchTarget0 = -1;
        instr.BranchTarget1 = -1;
        return instr;
    }

    public static IRInstruction CreateUnary(int id, IROp op, int resultId, int operand)
    {
        var instr = new IRInstruction();
        instr.Id = id;
        instr.Op = op;
        instr.ResultId = resultId;
        instr.OperandCount = 1;
        instr.Operands[0] = operand;
        instr.BranchTarget0 = -1;
        instr.BranchTarget1 = -1;
        return instr;
    }

    public static IRInstruction CreateBranch(int id, int targetBlock)
    {
        var instr = new IRInstruction();
        instr.Id = id;
        instr.Op = IROp.Branch;
        instr.ResultId = -1;
        instr.OperandCount = 0;
        instr.BranchTarget0 = targetBlock;
        instr.BranchTarget1 = -1;
        return instr;
    }

    public static IRInstruction CreateCondBranch(int id, IROp op, int condition, int trueBlock, int falseBlock)
    {
        var instr = new IRInstruction();
        instr.Id = id;
        instr.Op = op;
        instr.ResultId = -1;
        instr.OperandCount = 1;
        instr.Operands[0] = condition;
        instr.BranchTarget0 = trueBlock;
        instr.BranchTarget1 = falseBlock;
        return instr;
    }

    public static IRInstruction CreateReturn(int id, int valueId)
    {
        var instr = new IRInstruction();
        instr.Id = id;
        instr.Op = IROp.Return;
        instr.ResultId = -1;
        instr.OperandCount = valueId >= 0 ? (byte)1 : (byte)0;
        if (valueId >= 0) instr.Operands[0] = valueId;
        instr.BranchTarget0 = -1;
        instr.BranchTarget1 = -1;
        return instr;
    }

    public static IRInstruction CreatePhi(int id, int resultId)
    {
        var instr = new IRInstruction();
        instr.Id = id;
        instr.Op = IROp.Phi;
        instr.ResultId = resultId;
        instr.OperandCount = 0;
        instr.BranchTarget0 = -1;
        instr.BranchTarget1 = -1;
        return instr;
    }
}
