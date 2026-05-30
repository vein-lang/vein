namespace ishtar.jit;

using ishtar.collections;
using ishtar.runtime.gc;

/// <summary>
/// An IR function — the unit of compilation. Owns all IR data for one method.
/// All allocations go through the embedded allocator (GC-tracked arena).
/// </summary>
public unsafe struct IRFunction : IEq<IRFunction>, IEquatable<IRFunction>
{
    public int Id;
    public AllocatorBlock Allocator;

    /// <summary>All values (SSA defs) in this function.</summary>
    public IRValue* Values;
    public int ValueCount;
    public int ValueCapacity;

    /// <summary>All instructions in this function (flat array, referenced by ID).</summary>
    public IRInstruction* Instructions;
    public int InstructionCount;
    public int InstructionCapacity;

    /// <summary>All basic blocks.</summary>
    public IRBasicBlock* Blocks;
    public int BlockCount;
    public int BlockCapacity;

    /// <summary>Detected loops.</summary>
    public NativeList<IRLoop>* Loops;

    /// <summary>Entry block index (always 0).</summary>
    public int EntryBlockIndex;

    /// <summary>Argument count and types.</summary>
    public int ArgCount;
    public IRType* ArgTypes;

    /// <summary>Return type.</summary>
    public IRType ReturnType;

    /// <summary>Local variable count and types.</summary>
    public int LocalCount;
    public IRType* LocalTypes;

    // ═══════════════════════════════════════════════════════════════════
    // Lifecycle
    // ═══════════════════════════════════════════════════════════════════

    public static IRFunction* Create(AllocatorBlock allocator, int argCount, IRType* argTypes, IRType returnType)
    {
        var fn = (IRFunction*)allocator.alloc((uint)sizeof(IRFunction));
        fn->Id = 0;
        fn->Allocator = allocator;
        fn->ReturnType = returnType;
        fn->ArgCount = argCount;
        fn->EntryBlockIndex = 0;

        // Copy arg types
        fn->ArgTypes = (IRType*)allocator.alloc((uint)(argCount * sizeof(IRType)));
        for (var i = 0; i < argCount; i++)
            fn->ArgTypes[i] = argTypes[i];

        // Initial capacities
        fn->ValueCapacity = 64;
        fn->ValueCount = 0;
        fn->Values = (IRValue*)allocator.alloc((uint)(fn->ValueCapacity * sizeof(IRValue)));

        fn->InstructionCapacity = 128;
        fn->InstructionCount = 0;
        fn->Instructions = (IRInstruction*)allocator.alloc((uint)(fn->InstructionCapacity * sizeof(IRInstruction)));

        fn->BlockCapacity = 16;
        fn->BlockCount = 0;
        fn->Blocks = (IRBasicBlock*)allocator.alloc((uint)(fn->BlockCapacity * sizeof(IRBasicBlock)));

        fn->Loops = NativeList<IRLoop>.Create(4, allocator);

        fn->LocalCount = 0;
        fn->LocalTypes = null;

        return fn;
    }

    public static void Free(IRFunction* fn)
    {
        if (fn == null) return;
        var alloc = fn->Allocator;

        // Free per-block lists
        for (var i = 0; i < fn->BlockCount; i++)
        {
            var blk = &fn->Blocks[i];
            NativeList<IRInstrRef>.Free(blk->Instructions);
            NativeList<IRBlockRef>.Free(blk->Predecessors);
            NativeList<IRBlockRef>.Free(blk->Successors);
            NativeList<IRInstrRef>.Free(blk->PhiNodes);
        }

        NativeList<IRLoop>.Free(fn->Loops);
        alloc.free(fn->Values);
        alloc.free(fn->Instructions);
        alloc.free(fn->Blocks);
        alloc.free(fn->ArgTypes);
        if (fn->LocalTypes != null) alloc.free(fn->LocalTypes);
        alloc.free(fn);
    }

    // ═══════════════════════════════════════════════════════════════════
    // Value creation
    // ═══════════════════════════════════════════════════════════════════

    public int AllocValue(IRType type, int defBlock, int defInstr)
    {
        if (ValueCount == ValueCapacity)
            GrowValues();

        var id = ValueCount++;
        Values[id] = new IRValue { Id = id, Type = type, DefBlockIndex = defBlock, DefInstrIndex = defInstr };
        return id;
    }

    private void GrowValues()
    {
        ValueCapacity *= 2;
        Values = (IRValue*)Allocator.realloc(Values, (uint)(ValueCapacity * sizeof(IRValue)));
    }

    // ═══════════════════════════════════════════════════════════════════
    // Instruction creation
    // ═══════════════════════════════════════════════════════════════════

    public int AddInstruction(IRInstruction instr)
    {
        if (InstructionCount == InstructionCapacity)
            GrowInstructions();

        var id = InstructionCount++;
        instr.Id = id;
        Instructions[id] = instr;
        return id;
    }

    private void GrowInstructions()
    {
        InstructionCapacity *= 2;
        Instructions = (IRInstruction*)Allocator.realloc(Instructions, (uint)(InstructionCapacity * sizeof(IRInstruction)));
    }

    // ═══════════════════════════════════════════════════════════════════
    // Block creation
    // ═══════════════════════════════════════════════════════════════════

    public int AddBlock()
    {
        if (BlockCount == BlockCapacity)
            GrowBlocks();

        var idx = BlockCount++;
        var blk = &Blocks[idx];
        blk->Index = idx;
        blk->ImmediateDominator = -1;
        blk->LoopDepth = 0;
        blk->LoopHeaderIndex = -1;
        blk->Instructions = NativeList<IRInstrRef>.Create(16, Allocator);
        blk->Predecessors = NativeList<IRBlockRef>.Create(4, Allocator);
        blk->Successors = NativeList<IRBlockRef>.Create(4, Allocator);
        blk->PhiNodes = NativeList<IRInstrRef>.Create(2, Allocator);
        return idx;
    }

    private void GrowBlocks()
    {
        BlockCapacity *= 2;
        Blocks = (IRBasicBlock*)Allocator.realloc(Blocks, (uint)(BlockCapacity * sizeof(IRBasicBlock)));
    }

    // ═══════════════════════════════════════════════════════════════════
    // Helpers
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>Append an instruction to a block and link it.</summary>
    public void AppendToBlock(int blockIdx, int instrId)
    {
        var instrRef = (IRInstrRef*)Allocator.alloc((uint)sizeof(IRInstrRef));
        *instrRef = new IRInstrRef(instrId);
        Blocks[blockIdx].Instructions->Add(instrRef);
        Instructions[instrId].BlockIndex = blockIdx;
    }

    /// <summary>Add a CFG edge between two blocks.</summary>
    public void AddEdge(int fromBlock, int toBlock)
    {
        var succRef = (IRBlockRef*)Allocator.alloc((uint)sizeof(IRBlockRef));
        *succRef = new IRBlockRef(toBlock);
        var predRef = (IRBlockRef*)Allocator.alloc((uint)sizeof(IRBlockRef));
        *predRef = new IRBlockRef(fromBlock);
        Blocks[fromBlock].Successors->Add(succRef);
        Blocks[toBlock].Predecessors->Add(predRef);
    }

    /// <summary>Get the last instruction in a block, or null if the block is empty.</summary>
    public IRInstruction* GetLastInstructionInBlock(int blockIdx)
    {
        var blk = &Blocks[blockIdx];
        if (blk->Instructions->Count == 0) return null;
        var lastRef = blk->Instructions->Get(blk->Instructions->Count - 1);
        return &Instructions[lastRef->Id];
    }

    /// <summary>Replace all uses of a value with another value across all instructions.</summary>
    public void ReplaceAllUses(int oldValueId, int newValueId)
    {
        for (var i = 0; i < InstructionCount; i++)
        {
            var instr = &Instructions[i];
            if (instr->IsDead) continue;
            for (var j = 0; j < instr->OperandCount; j++)
            {
                if (instr->GetOperand(j) == oldValueId)
                    instr->SetOperand(j, newValueId);
            }
        }
    }

    /// <summary>Set local variable metadata.</summary>
    public void SetLocals(int count, IRType* types)
    {
        LocalCount = count;
        LocalTypes = (IRType*)Allocator.alloc((uint)(count * sizeof(IRType)));
        for (var i = 0; i < count; i++)
            LocalTypes[i] = types[i];
    }

    public static bool Eq(IRFunction* p1, IRFunction* p2) => p1->Id == p2->Id;
    public bool Equals(IRFunction other) => Id == other.Id;
}
