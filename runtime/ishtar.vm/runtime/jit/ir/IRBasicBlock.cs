namespace ishtar.jit;

using ishtar.collections;

/// <summary>
/// A basic block in the CFG. Unmanaged struct — allocated from IR arena.
/// Contains a linear sequence of instruction indices into the IRFunction's instruction table.
/// The last instruction is always a terminator (Branch, BranchTrue/False, Return).
/// </summary>
public unsafe struct IRBasicBlock : IEq<IRBasicBlock>, IEquatable<IRBasicBlock>
{
    public int Index;

    /// <summary>Instruction IDs in this block (indices into IRFunction.Instructions).</summary>
    public NativeList<IRInstrRef>* Instructions;

    /// <summary>Predecessor block indices.</summary>
    public NativeList<IRBlockRef>* Predecessors;

    /// <summary>Successor block indices.</summary>
    public NativeList<IRBlockRef>* Successors;

    /// <summary>Phi instruction IDs at the beginning of this block.</summary>
    public NativeList<IRInstrRef>* PhiNodes;

    /// <summary>Immediate dominator block index (-1 if entry).</summary>
    public int ImmediateDominator;

    /// <summary>Loop nesting depth (0 = not in loop).</summary>
    public int LoopDepth;

    /// <summary>Loop header block index (-1 if not in a loop).</summary>
    public int LoopHeaderIndex;

    public static bool Eq(IRBasicBlock* p1, IRBasicBlock* p2) => p1->Index == p2->Index;
    public bool Equals(IRBasicBlock other) => Index == other.Index;
}

/// <summary>Thin wrapper for instruction index to satisfy IEq constraint for NativeList.</summary>
public unsafe struct IRInstrRef : IEq<IRInstrRef>, IEquatable<IRInstrRef>
{
    public int Id;
    public IRInstrRef(int id) => Id = id;
    public static bool Eq(IRInstrRef* p1, IRInstrRef* p2) => p1->Id == p2->Id;
    public bool Equals(IRInstrRef other) => Id == other.Id;
    public static implicit operator int(IRInstrRef r) => r.Id;
    public static implicit operator IRInstrRef(int id) => new(id);
}

/// <summary>Thin wrapper for block index to satisfy IEq constraint for NativeList.</summary>
public unsafe struct IRBlockRef : IEq<IRBlockRef>, IEquatable<IRBlockRef>
{
    public int Id;
    public IRBlockRef(int id) => Id = id;
    public static bool Eq(IRBlockRef* p1, IRBlockRef* p2) => p1->Id == p2->Id;
    public bool Equals(IRBlockRef other) => Id == other.Id;
    public static implicit operator int(IRBlockRef r) => r.Id;
    public static implicit operator IRBlockRef(int id) => new(id);
}

/// <summary>
/// Represents a natural loop in the CFG.
/// </summary>
public unsafe struct IRLoop : IEq<IRLoop>, IEquatable<IRLoop>
{
    public int HeaderBlockIndex;
    public NativeList<IRBlockRef>* BodyBlocks;
    public NativeList<IRBlockRef>* BackEdges;
    public NativeList<IRBlockRef>* ExitBlocks;
    public int ParentLoopIndex; // -1 if top-level
    public int Depth;

    /// <summary>Induction variable value ID (-1 if not detected).</summary>
    public int InductionVarId;
    /// <summary>Trip count (-1 if unknown).</summary>
    public long TripCount;

    public static bool Eq(IRLoop* p1, IRLoop* p2) => p1->HeaderBlockIndex == p2->HeaderBlockIndex;
    public bool Equals(IRLoop other) => HeaderBlockIndex == other.HeaderBlockIndex;
}
