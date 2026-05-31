namespace ishtar.jit;

/// <summary>
/// IR opcode — platform-independent operations.
/// These map from Vein bytecode into a lower-level SSA representation.
/// </summary>
public enum IROp
{
    // ─── Constants ─────────────────────────────────────────────
    Const,          // immediate constant value
    Phi,            // SSA φ-node (merge point)

    // ─── Arithmetic ────────────────────────────────────────────
    Add,
    Sub,
    Mul,
    Div,
    Mod,
    Neg,

    // ─── Bitwise ───────────────────────────────────────────────
    And,
    Or,
    Xor,
    Shl,
    Shr,
    Not,

    // ─── Comparison (produces Bool) ────────────────────────────
    CmpEq,
    CmpNe,
    CmpLt,
    CmpLe,
    CmpGt,
    CmpGe,

    // ─── Conversion ────────────────────────────────────────────
    Conv,           // type conversion / widening / narrowing

    // ─── Memory ────────────────────────────────────────────────
    Load,           // load from address
    Store,          // store to address
    LoadField,      // object field load
    StoreField,     // object field store
    LoadElem,       // array element load
    StoreElem,      // array element store
    LoadArg,        // load method argument
    StoreArg,       // store method argument
    LoadLocal,      // load local variable
    StoreLocal,     // store local variable
    Alloc,          // object/array allocation

    // ─── Array ─────────────────────────────────────────────────
    ArrayLen,       // array length
    BoundsCheck,    // explicit bounds check (can be eliminated)

    // ─── Control Flow ──────────────────────────────────────────
    Branch,         // unconditional branch
    BranchTrue,     // conditional branch (if true)
    BranchFalse,    // conditional branch (if false)
    Return,         // method return

    // ─── Call ──────────────────────────────────────────────────
    Call,           // direct call
    CallVirt,       // virtual dispatch call
    CallIndirect,   // indirect call via function pointer

    // ─── Object Model ──────────────────────────────────────────
    NewObj,         // allocate object
    NewArr,         // allocate array
    Cast,           // type cast (may throw)
    IsInst,         // type check (null if fail)
    Null,           // null constant

    // ─── Struct Operations ─────────────────────────────────────
    InitStruct,     // allocate + zero-init struct (MethodRef = class ptr)
    CopyStruct,     // deep-copy struct (MethodRef = class ptr, operand[0] = src)
    Box,            // box value type (MethodRef = class ptr, operand[0] = value)
    Unbox,          // unbox object to value (MethodRef = class ptr, operand[0] = obj)

    // ─── Misc ──────────────────────────────────────────────────
    Nop,
    Dup,
}
