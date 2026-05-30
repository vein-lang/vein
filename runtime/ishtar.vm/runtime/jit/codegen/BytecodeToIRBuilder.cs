namespace ishtar.jit;

using ishtar;
using ishtar.collections;
using ishtar.runtime;
using ishtar.runtime.gc;
using vein.runtime;

/// <summary>
/// Translates Vein stack-based bytecode into SSA-form IR.
/// Simulates the operand stack abstractly, converting push/pop patterns into
/// explicit data-flow edges between IRValues.
/// </summary>
public static unsafe class BytecodeToIRBuilder
{
    /// <summary>
    /// Build IR from a method's bytecode, with module context for CALL resolution.
    /// </summary>
    public static IRFunction* Build(RuntimeIshtarMethod* method, AllocatorBlock allocator)
    {
        var header = method->Header;
        var argCount = method->ArgLength;

        var argTypes = stackalloc IRType[argCount];
        for (var i = 0; i < argCount; i++)
            argTypes[i] = IRTypeMap.FromVein(method->Arguments->Get(i)->Type.Class->TypeCode);

        var returnType = IRTypeMap.FromVein(method->ReturnType->TypeCode);

        var module = method->Owner->Owner;

        // Detect if this method has only tail-recursive self-calls (eligible for TCO)
        var isTailRecursive = MethodCompiler.HasOnlyTailSelfCalls(method);

        return Build(header->code, header->code_size, argCount, argTypes, returnType, header->max_stack, allocator, module,
            method, isTailRecursive, header);
    }

    /// <summary>
    /// Build IR from raw bytecode. Test-friendly overload (no CALL support without module).
    /// When selfMethod is non-null, self-calls become either TCO loops (if isTailRecursive) or indirect recursive calls.
    /// </summary>
    public static IRFunction* Build(uint* code, uint codeSize, int argCount, IRType* argTypes, IRType returnType, int maxStack, AllocatorBlock allocator, RuntimeIshtarModule* module = null, RuntimeIshtarMethod* selfMethod = null, bool isTailRecursive = false, MetaMethodHeader* header = null)
    {
        var fn = IRFunction.Create(allocator, argCount, argTypes, returnType);

        // Create entry block
        var entryBlock = fn->AddBlock();

        // For tail-recursive methods: entry just branches to loop_header where LoadArgs live
        // For normal methods: entry IS the body
        int loopHeaderBlock;

        if (isTailRecursive)
        {
            loopHeaderBlock = fn->AddBlock();
            // Entry branches to loop header
            var branchInstr = new IRInstruction();
            branchInstr.Op = IROp.Branch;
            branchInstr.ResultId = -1;
            branchInstr.BranchTarget0 = loopHeaderBlock;
            branchInstr.BranchTarget1 = -1;
            branchInstr.OperandCount = 0;
            var brId = fn->AddInstruction(branchInstr);
            fn->AppendToBlock(entryBlock, brId);
        }
        else
        {
            loopHeaderBlock = entryBlock;
        }

        // Abstract operand stack (value IDs)
        var stack = stackalloc int[maxStack + 16];
        var sp = 0;

        // Create values for arguments (pre-defined) — in loop_header block
        var argValueIds = stackalloc int[argCount];
        for (var i = 0; i < argCount; i++)
        {
            var valId = fn->AllocValue(argTypes[i], loopHeaderBlock, -1);
            argValueIds[i] = valId;

            // Emit LoadArg instruction
            var instr = new IRInstruction();
            instr.Op = IROp.LoadArg;
            instr.ResultId = valId;
            instr.Immediate = i;
            instr.OperandCount = 0;
            instr.BranchTarget0 = -1;
            instr.BranchTarget1 = -1;
            var instrId = fn->AddInstruction(instr);
            fn->AppendToBlock(loopHeaderBlock, instrId);
            fn->Values[valId].DefInstrIndex = instrId;
        }

        // ─── Label resolution: map bytecode positions to IR blocks ───
        // Pre-scan jump targets to create blocks at the right positions
        const int maxLabelTargets = 128;
        var targetPositions = stackalloc int[maxLabelTargets]; // bytecodePosition values that are jump targets
        var targetBlocks = stackalloc int[maxLabelTargets];    // corresponding IR block IDs
        var targetCount = 0;

        if (header != null && header->labels != null && header->labels_map != null)
        {
            // Scan bytecode for JMP/JMP_F/JMP_T to collect all target positions
            var scanIp = code;
            var scanEnd = code + codeSize;
            while (scanIp < scanEnd)
            {
                var scanOp = (OpCodeValue)(ushort)*scanIp;
                scanIp++;
                switch (scanOp)
                {
                    case OpCodeValue.JMP:
                    case OpCodeValue.JMP_F:
                    case OpCodeValue.JMP_T:
                    {
                        var labelIdx = (int)*scanIp; scanIp++;
                        var labelKey = header->labels->Get(labelIdx);
                        if (header->labels_map->TryGetValue(labelKey, out var label))
                        {
                            // VM uses: ip = start + label.pos - 1 (pos is 1-based)
                            var pos = label.pos - 1;
                            // Check if this position is already in our list
                            var found = false;
                            for (var t = 0; t < targetCount; t++)
                            {
                                if (targetPositions[t] == pos) { found = true; break; }
                            }
                            if (!found && targetCount < maxLabelTargets)
                            {
                                targetPositions[targetCount] = pos;
                                targetBlocks[targetCount] = fn->AddBlock();
                                targetCount++;
                            }
                        }
                        break;
                    }
                    // Skip operands for other opcodes
                    case OpCodeValue.LDARG_S:
                    case OpCodeValue.LDLOC_S:
                    case OpCodeValue.STLOC_S:
                    case OpCodeValue.LDC_I4_S:
                    case OpCodeValue.LDC_I2_S:
                    case OpCodeValue.LDC_F4:
                        scanIp++;
                        break;
                    case OpCodeValue.LDC_I8_S:
                    case OpCodeValue.CALL:
                        scanIp += 2;
                        break;
                    case OpCodeValue.LOC_INIT:
                    {
                        var c = (int)*scanIp; scanIp++;
                        scanIp += c * 2; // each entry is 2 words (type idx + padding)
                        break;
                    }
                    default:
                        break;
                }
            }
        }

        // Walk bytecode
        var ip = code;
        var end = code + codeSize;
        var currentBlock = loopHeaderBlock;

        // Locals: track the current SSA value for each local slot
        // (updated on STLOC, read on LDLOC)
        const int maxLocals = 256;
        var localValueIds = stackalloc int[maxLocals];
        for (var i = 0; i < maxLocals; i++) localValueIds[i] = -1;

        while (ip < end)
        {
            // Check if this bytecode position is a jump target — if so, switch to that block
            var currentPos = (int)(ip - code);
            for (var t = 0; t < targetCount; t++)
            {
                if (targetPositions[t] == currentPos)
                {
                    // Emit fall-through branch to the target block (if current block isn't already terminated)
                    var lastInstr = fn->GetLastInstructionInBlock(currentBlock);
                    if (lastInstr == null || (lastInstr->Op != IROp.Branch && lastInstr->Op != IROp.BranchTrue &&
                                              lastInstr->Op != IROp.BranchFalse && lastInstr->Op != IROp.Return))
                    {
                        var fallBr = new IRInstruction();
                        fallBr.Op = IROp.Branch;
                        fallBr.ResultId = -1;
                        fallBr.BranchTarget0 = targetBlocks[t];
                        fallBr.BranchTarget1 = -1;
                        fallBr.OperandCount = 0;
                        var fallBrId = fn->AddInstruction(fallBr);
                        fn->AppendToBlock(currentBlock, fallBrId);
                    }
                    currentBlock = targetBlocks[t];
                    break;
                }
            }

            var opcode = (OpCodeValue)(ushort)*ip;
            ip++;

            switch (opcode)
            {
                case OpCodeValue.NOP:
                    break;

                // ─── Arithmetic ──────────────────────────────────────
                case OpCodeValue.ADD:
                    EmitBinary(fn, IROp.Add, stack, &sp, currentBlock);
                    break;
                case OpCodeValue.SUB:
                    EmitBinary(fn, IROp.Sub, stack, &sp, currentBlock);
                    break;
                case OpCodeValue.MUL:
                    EmitBinary(fn, IROp.Mul, stack, &sp, currentBlock);
                    break;
                case OpCodeValue.DIV:
                    EmitBinary(fn, IROp.Div, stack, &sp, currentBlock);
                    break;
                case OpCodeValue.MOD:
                    EmitBinary(fn, IROp.Mod, stack, &sp, currentBlock);
                    break;

                // ─── Bitwise ─────────────────────────────────────────
                case OpCodeValue.XOR:
                    EmitBinary(fn, IROp.Xor, stack, &sp, currentBlock);
                    break;
                case OpCodeValue.OR:
                    EmitBinary(fn, IROp.Or, stack, &sp, currentBlock);
                    break;
                case OpCodeValue.AND:
                    EmitBinary(fn, IROp.And, stack, &sp, currentBlock);
                    break;
                case OpCodeValue.SHR:
                    EmitBinary(fn, IROp.Shr, stack, &sp, currentBlock);
                    break;
                case OpCodeValue.SHL:
                    EmitBinary(fn, IROp.Shl, stack, &sp, currentBlock);
                    break;

                // ─── Load arguments ──────────────────────────────────
                case OpCodeValue.LDARG_0: stack[sp++] = argValueIds[0]; break;
                case OpCodeValue.LDARG_1: stack[sp++] = argValueIds[1]; break;
                case OpCodeValue.LDARG_2: stack[sp++] = argValueIds[2]; break;
                case OpCodeValue.LDARG_3: stack[sp++] = argValueIds[3]; break;
                case OpCodeValue.LDARG_4: stack[sp++] = argValueIds[4]; break;
                case OpCodeValue.LDARG_5: stack[sp++] = argValueIds[5]; break;
                case OpCodeValue.LDARG_S:
                {
                    var idx = (int)*ip; ip++;
                    stack[sp++] = argValueIds[idx];
                    break;
                }

                // ─── Locals ──────────────────────────────────────────
                case OpCodeValue.LOC_INIT:
                {
                    // LOC_INIT: count, then 2 words per entry (type idx + skip)
                    var localsCount = (int)*ip; ip++;
                    for (var i = 0; i < localsCount; i++)
                    {
                        ip += 2; // each entry is 2 words in bytecode
                        localValueIds[i] = -1;
                    }
                    break;
                }

                case OpCodeValue.LDLOC_0: stack[sp++] = localValueIds[0]; break;
                case OpCodeValue.LDLOC_1: stack[sp++] = localValueIds[1]; break;
                case OpCodeValue.LDLOC_2: stack[sp++] = localValueIds[2]; break;
                case OpCodeValue.LDLOC_3: stack[sp++] = localValueIds[3]; break;
                case OpCodeValue.LDLOC_4: stack[sp++] = localValueIds[4]; break;
                case OpCodeValue.LDLOC_5: stack[sp++] = localValueIds[5]; break;
                case OpCodeValue.LDLOC_S:
                {
                    var idx = (int)*ip; ip++;
                    stack[sp++] = localValueIds[idx];
                    break;
                }

                case OpCodeValue.STLOC_0: localValueIds[0] = stack[--sp]; break;
                case OpCodeValue.STLOC_1: localValueIds[1] = stack[--sp]; break;
                case OpCodeValue.STLOC_2: localValueIds[2] = stack[--sp]; break;
                case OpCodeValue.STLOC_3: localValueIds[3] = stack[--sp]; break;
                case OpCodeValue.STLOC_4: localValueIds[4] = stack[--sp]; break;
                case OpCodeValue.STLOC_5: localValueIds[5] = stack[--sp]; break;
                case OpCodeValue.STLOC_S:
                {
                    var idx = (int)*ip; ip++;
                    localValueIds[idx] = stack[--sp];
                    break;
                }

                // ─── Load constants ──────────────────────────────────
                case OpCodeValue.LDC_I4_0: EmitConst(fn, 0, IRType.I4, stack, &sp, currentBlock); break;
                case OpCodeValue.LDC_I4_1: EmitConst(fn, 1, IRType.I4, stack, &sp, currentBlock); break;
                case OpCodeValue.LDC_I4_2: EmitConst(fn, 2, IRType.I4, stack, &sp, currentBlock); break;
                case OpCodeValue.LDC_I4_3: EmitConst(fn, 3, IRType.I4, stack, &sp, currentBlock); break;
                case OpCodeValue.LDC_I4_4: EmitConst(fn, 4, IRType.I4, stack, &sp, currentBlock); break;
                case OpCodeValue.LDC_I4_5: EmitConst(fn, 5, IRType.I4, stack, &sp, currentBlock); break;
                case OpCodeValue.LDC_I4_S:
                {
                    var val = (int)*ip; ip++;
                    EmitConst(fn, val, IRType.I4, stack, &sp, currentBlock);
                    break;
                }

                case OpCodeValue.LDC_I8_0: EmitConst(fn, 0, IRType.I8, stack, &sp, currentBlock); break;
                case OpCodeValue.LDC_I8_1: EmitConst(fn, 1, IRType.I8, stack, &sp, currentBlock); break;
                case OpCodeValue.LDC_I8_2: EmitConst(fn, 2, IRType.I8, stack, &sp, currentBlock); break;
                case OpCodeValue.LDC_I8_3: EmitConst(fn, 3, IRType.I8, stack, &sp, currentBlock); break;
                case OpCodeValue.LDC_I8_4: EmitConst(fn, 4, IRType.I8, stack, &sp, currentBlock); break;
                case OpCodeValue.LDC_I8_5: EmitConst(fn, 5, IRType.I8, stack, &sp, currentBlock); break;
                case OpCodeValue.LDC_I8_S:
                {
                    var lo = (uint)*ip; ip++;
                    var hi = (uint)*ip; ip++;
                    var val = (long)lo | ((long)hi << 32);
                    EmitConst(fn, val, IRType.I8, stack, &sp, currentBlock);
                    break;
                }

                case OpCodeValue.LDC_I2_0: EmitConst(fn, 0, IRType.I2, stack, &sp, currentBlock); break;
                case OpCodeValue.LDC_I2_1: EmitConst(fn, 1, IRType.I2, stack, &sp, currentBlock); break;
                case OpCodeValue.LDC_I2_2: EmitConst(fn, 2, IRType.I2, stack, &sp, currentBlock); break;
                case OpCodeValue.LDC_I2_3: EmitConst(fn, 3, IRType.I2, stack, &sp, currentBlock); break;
                case OpCodeValue.LDC_I2_4: EmitConst(fn, 4, IRType.I2, stack, &sp, currentBlock); break;
                case OpCodeValue.LDC_I2_5: EmitConst(fn, 5, IRType.I2, stack, &sp, currentBlock); break;
                case OpCodeValue.LDC_I2_S:
                {
                    var val = (short)(ushort)*ip; ip++;
                    EmitConst(fn, val, IRType.I2, stack, &sp, currentBlock);
                    break;
                }

                case OpCodeValue.LDC_F4:
                {
                    var bits = (int)*ip; ip++;
                    EmitConst(fn, bits, IRType.R4, stack, &sp, currentBlock);
                    break;
                }

                // ─── Comparisons ─────────────────────────────────────
                case OpCodeValue.EQL_T:
                    EmitBinary(fn, IROp.CmpEq, stack, &sp, currentBlock);
                    break;
                case OpCodeValue.EQL_F:
                    EmitBinary(fn, IROp.CmpNe, stack, &sp, currentBlock);
                    break;
                case OpCodeValue.EQL_NQ:
                    EmitBinary(fn, IROp.CmpNe, stack, &sp, currentBlock);
                    break;
                case OpCodeValue.EQL_L:
                    EmitBinary(fn, IROp.CmpLt, stack, &sp, currentBlock);
                    break;
                case OpCodeValue.EQL_LQ:
                    EmitBinary(fn, IROp.CmpLe, stack, &sp, currentBlock);
                    break;
                case OpCodeValue.EQL_H:
                    EmitBinary(fn, IROp.CmpGt, stack, &sp, currentBlock);
                    break;
                case OpCodeValue.EQL_HQ:
                    EmitBinary(fn, IROp.CmpGe, stack, &sp, currentBlock);
                    break;

                // ─── Branches ────────────────────────────────────────
                case OpCodeValue.JMP:
                {
                    var labelIdx = (int)*ip; ip++;
                    var targetBlock = ResolveBranchTarget(labelIdx, header, targetPositions, targetBlocks, targetCount);
                    if (targetBlock < 0) targetBlock = fn->AddBlock(); // unresolvable label (dead code)
                    var instr = IRInstruction.CreateBranch(0, targetBlock);
                    var instrId = fn->AddInstruction(instr);
                    fn->AppendToBlock(currentBlock, instrId);
                    // Start a new block for fall-through (if any)
                    currentBlock = fn->AddBlock();
                    break;
                }

                case OpCodeValue.JMP_T:
                {
                    var labelIdx = (int)*ip; ip++;
                    var cond = stack[--sp];
                    var falseBlock = fn->AddBlock();
                    var targetBlock = ResolveBranchTarget(labelIdx, header, targetPositions, targetBlocks, targetCount);
                    if (targetBlock < 0) targetBlock = fn->AddBlock(); // unresolvable label (dead code)
                    var instr = IRInstruction.CreateCondBranch(0, IROp.BranchTrue, cond, targetBlock, falseBlock);
                    var instrId = fn->AddInstruction(instr);
                    fn->AppendToBlock(currentBlock, instrId);
                    currentBlock = falseBlock;
                    break;
                }

                case OpCodeValue.JMP_F:
                {
                    var labelIdx = (int)*ip; ip++;
                    var cond = stack[--sp];
                    var falseBlock = fn->AddBlock();
                    var targetBlock = ResolveBranchTarget(labelIdx, header, targetPositions, targetBlocks, targetCount);
                    if (targetBlock < 0) targetBlock = fn->AddBlock(); // unresolvable label (dead code)
                    var instr = IRInstruction.CreateCondBranch(0, IROp.BranchFalse, cond, targetBlock, falseBlock);
                    var instrId = fn->AddInstruction(instr);
                    fn->AppendToBlock(currentBlock, instrId);
                    currentBlock = falseBlock;
                    break;
                }

                // ─── Return ──────────────────────────────────────────
                case OpCodeValue.RET:
                {
                    var retVal = returnType != IRType.Void ? stack[--sp] : -1;
                    var instr = IRInstruction.CreateReturn(0, retVal);
                    var instrId = fn->AddInstruction(instr);
                    fn->AppendToBlock(currentBlock, instrId);
                    currentBlock = fn->AddBlock(); // dead block after return
                    break;
                }

                // ─── Stack ops ───────────────────────────────────────
                case OpCodeValue.DUP:
                {
                    var top = stack[sp - 1];
                    stack[sp++] = top;
                    break;
                }

                case OpCodeValue.POP:
                    sp--;
                    break;

                // ─── Array ───────────────────────────────────────────
                case OpCodeValue.LDLEN:
                {
                    var arr = stack[--sp];
                    var valId = fn->AllocValue(IRType.I4, currentBlock, fn->InstructionCount);
                    var instr = IRInstruction.CreateUnary(0, IROp.ArrayLen, valId, arr);
                    var instrId = fn->AddInstruction(instr);
                    fn->AppendToBlock(currentBlock, instrId);
                    fn->Values[valId].DefInstrIndex = instrId;
                    stack[sp++] = valId;
                    break;
                }

                // ─── Call ────────────────────────────────────────────
                case OpCodeValue.CALL:
                {
                    // CALL format: tokenIdx, ownerTypeIdx
                    var tokenIdx = *ip; ip++;
                    var ownerIdx = *ip; ip++;

                    if (module == null)
                        break; // no module context — can't resolve

                    // Resolve target method at compile time
                    RuntimeIshtarMethod* target = null;
                    if (module->types_table->TryGetValue((int)ownerIdx, out var ownerName))
                    {
                        var ownerClass = module->FindType(ownerName, true, false);
                        if (ownerClass != null && !ownerClass->IsUnresolved)
                        {
                            var methodName = module->GetConstStringByIndex((int)tokenIdx);
                            target = ownerClass->FindMethod(methodName, m => m->Name.Equals(methodName));
                        }
                    }

                    if (target == null)
                        break; // resolution failed — will be caught by eligibility check

                    // ─── Tail-recursive self-call: CALL self followed by RET → loop ───
                    if (isTailRecursive && target == selfMethod &&
                        ip < end && (OpCodeValue)(ushort)*ip == OpCodeValue.RET)
                    {
                        // Pop arguments from abstract stack (reverse order)
                        var callArgCount = target->ArgLength;
                        var callArgIds = stackalloc int[callArgCount];
                        for (var i = callArgCount - 1; i >= 0; i--)
                            callArgIds[i] = stack[--sp];

                        // Emit StoreArg for each argument (write new values to r14)
                        for (var i = 0; i < callArgCount; i++)
                        {
                            var storeInstr = new IRInstruction();
                            storeInstr.Op = IROp.StoreArg;
                            storeInstr.ResultId = -1;
                            storeInstr.Immediate = i;
                            storeInstr.OperandCount = 1;
                            storeInstr.Operands[0] = callArgIds[i];
                            storeInstr.BranchTarget0 = -1;
                            storeInstr.BranchTarget1 = -1;
                            var stId = fn->AddInstruction(storeInstr);
                            fn->AppendToBlock(currentBlock, stId);
                        }

                        // Branch back to loop header (re-reads args from r14)
                        var brInstr = new IRInstruction();
                        brInstr.Op = IROp.Branch;
                        brInstr.ResultId = -1;
                        brInstr.BranchTarget0 = loopHeaderBlock;
                        brInstr.BranchTarget1 = -1;
                        brInstr.OperandCount = 0;
                        var brId = fn->AddInstruction(brInstr);
                        fn->AppendToBlock(currentBlock, brId);

                        // Skip the RET opcode (already consumed by tail call)
                        ip++;

                        // Start dead block after tail call
                        currentBlock = fn->AddBlock();
                        break;
                    }

                    // ─── Non-tail self-call: indirect recursive call ───
                    if (target == selfMethod)
                    {
                        // Emit CallIndirect — at runtime, loads fn ptr from &method->PIInfo.compiled_func_ref
                        var selfSlotAddr = (nint)(&selfMethod->PIInfo.compiled_func_ref);

                        var indArgCount = target->ArgLength;
                        var indArgIds = stackalloc int[indArgCount];
                        for (var i = indArgCount - 1; i >= 0; i--)
                            indArgIds[i] = stack[--sp];

                        var indReturnType = IRTypeMap.FromVein(target->ReturnType->TypeCode);
                        var indHasResult = indReturnType != IRType.Void;

                        var indResultId = indHasResult
                            ? fn->AllocValue(indReturnType, currentBlock, fn->InstructionCount)
                            : -1;

                        var indInstr = new IRInstruction();
                        indInstr.Op = IROp.CallIndirect;
                        indInstr.ResultId = indResultId;
                        indInstr.MethodRef = selfSlotAddr;
                        indInstr.Immediate = indArgCount;
                        indInstr.OperandCount = (byte)indArgCount;
                        indInstr.BranchTarget0 = -1;
                        indInstr.BranchTarget1 = -1;
                        for (var i = 0; i < indArgCount && i < 4; i++)
                            indInstr.Operands[i] = indArgIds[i];

                        var indInstrId = fn->AddInstruction(indInstr);
                        fn->AppendToBlock(currentBlock, indInstrId);
                        if (indHasResult)
                        {
                            fn->Values[indResultId].DefInstrIndex = indInstrId;
                            stack[sp++] = indResultId;
                        }
                        break;
                    }

                    // ─── Normal call (target must be already jitted) ───
                    if (!target->IsJitted)
                        break; // target not yet compiled — method not eligible

                    var targetFnPtr = target->PIInfo.compiled_func_ref;
                        break; // can't JIT target — fall back

                    // Pop arguments from abstract stack (in reverse order — last arg is on top)
                    var callArgCount2 = target->ArgLength;
                    var callArgIds2 = stackalloc int[callArgCount2];
                    for (var i = callArgCount2 - 1; i >= 0; i--)
                        callArgIds2[i] = stack[--sp];

                    // Determine result type
                    var callReturnType = IRTypeMap.FromVein(target->ReturnType->TypeCode);
                    var hasResult = callReturnType != IRType.Void;

                    // Create Call IR instruction
                    var callResultId = hasResult
                        ? fn->AllocValue(callReturnType, currentBlock, fn->InstructionCount)
                        : -1;

                    var callInstr = new IRInstruction();
                    callInstr.Op = IROp.Call;
                    callInstr.ResultId = callResultId;
                    callInstr.MethodRef = targetFnPtr;
                    callInstr.Immediate = callArgCount2;
                    callInstr.OperandCount = (byte)callArgCount2;
                    callInstr.BranchTarget0 = -1;
                    callInstr.BranchTarget1 = -1;
                    for (var i = 0; i < callArgCount2 && i < 4; i++)
                        callInstr.Operands[i] = callArgIds2[i];
                    // TODO: handle > 4 args via OperandsExtra

                    var callInstrId = fn->AddInstruction(callInstr);
                    fn->AppendToBlock(currentBlock, callInstrId);
                    if (hasResult)
                    {
                        fn->Values[callResultId].DefInstrIndex = callInstrId;
                        stack[sp++] = callResultId;
                    }
                    break;
                }

                default:
                    // Unhandled opcode — emit Nop and skip operands
                    // TODO: handle remaining opcodes as the JIT matures
                    break;
            }
        }

        return fn;
    }

    /// <summary>
    /// Resolve a branch label index to an IR block ID using the pre-scanned target map.
    /// Falls back to using the label index directly if no header is available (test mode).
    /// </summary>
    private static int ResolveBranchTarget(int labelIdx, MetaMethodHeader* header,
        int* targetPositions, int* targetBlocks, int targetCount)
    {
        if (header == null || header->labels == null || header->labels_map == null)
            return labelIdx; // test mode — use raw value

        var labelKey = header->labels->Get(labelIdx);
        if (!header->labels_map->TryGetValue(labelKey, out var label))
            return -1; // should not happen for valid bytecode

        // VM uses: ip = start + label.pos - 1 (pos is 1-based)
        var pos = label.pos - 1;
        for (var t = 0; t < targetCount; t++)
        {
            if (targetPositions[t] == pos)
                return targetBlocks[t];
        }

        return -1; // should not happen if pre-scan was complete
    }

    private static void EmitBinary(IRFunction* fn, IROp op, int* stack, int* sp, int blockIdx)
    {
        var rhs = stack[--(*sp)];
        var lhs = stack[--(*sp)];

        // Determine result type (take the wider of two operands)
        var lhsType = fn->Values[lhs].Type;
        var rhsType = fn->Values[rhs].Type;
        var resultType = WiderType(lhsType, rhsType);

        // For comparisons, result is always Bool
        if (op is IROp.CmpEq or IROp.CmpNe or IROp.CmpLt or IROp.CmpLe or IROp.CmpGt or IROp.CmpGe)
            resultType = IRType.Bool;

        var valId = fn->AllocValue(resultType, blockIdx, fn->InstructionCount);
        var instr = IRInstruction.CreateBinary(0, op, valId, lhs, rhs);
        var instrId = fn->AddInstruction(instr);
        fn->AppendToBlock(blockIdx, instrId);
        fn->Values[valId].DefInstrIndex = instrId;
        stack[(*sp)++] = valId;
    }

    private static void EmitConst(IRFunction* fn, long value, IRType type, int* stack, int* sp, int blockIdx)
    {
        var valId = fn->AllocValue(type, blockIdx, fn->InstructionCount);
        var instr = IRInstruction.CreateConst(0, valId, value, type);
        var instrId = fn->AddInstruction(instr);
        fn->AppendToBlock(blockIdx, instrId);
        fn->Values[valId].DefInstrIndex = instrId;
        stack[(*sp)++] = valId;
    }

    private static IRType WiderType(IRType a, IRType b)
    {
        if (a == b) return a;
        if (IRTypeMap.IsFloat(a) || IRTypeMap.IsFloat(b))
            return IRTypeMap.SizeOf(a) >= IRTypeMap.SizeOf(b) ? a : b;
        return IRTypeMap.SizeOf(a) >= IRTypeMap.SizeOf(b) ? a : b;
    }
}
