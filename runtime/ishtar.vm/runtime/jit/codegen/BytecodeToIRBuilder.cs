namespace ishtar.jit;

using ishtar.collections;
using vein.runtime;

/// <summary>
/// Translates Vein stack-based bytecode into SSA-form IR.
/// Simulates the operand stack abstractly, converting push/pop patterns into
/// explicit data-flow edges between IRValues.
/// </summary>
public static unsafe class BytecodeToIRBuilder
{
    /// <summary>
    /// Build IR from a method's bytecode.
    /// </summary>
    public static IRFunction* Build(RuntimeIshtarMethod* method, AllocatorBlock allocator)
    {
        var header = method->Header;
        var argCount = method->ArgLength;

        var argTypes = stackalloc IRType[argCount];
        for (var i = 0; i < argCount; i++)
            argTypes[i] = IRTypeMap.FromVein(method->Arguments->Get(i)->Type.Class->TypeCode);

        var returnType = IRTypeMap.FromVein(method->ReturnType->TypeCode);

        return Build(header->code, header->code_size, argCount, argTypes, returnType, header->max_stack, allocator);
    }

    /// <summary>
    /// Build IR from raw bytecode. Test-friendly overload.
    /// </summary>
    public static IRFunction* Build(uint* code, uint codeSize, int argCount, IRType* argTypes, IRType returnType, int maxStack, AllocatorBlock allocator)
    {
        var fn = IRFunction.Create(allocator, argCount, argTypes, returnType);

        // Create entry block
        var entryBlock = fn->AddBlock();

        // Abstract operand stack (value IDs)
        var stack = stackalloc int[maxStack + 16];
        var sp = 0;

        // Create values for arguments (pre-defined)
        var argValueIds = stackalloc int[argCount];
        for (var i = 0; i < argCount; i++)
        {
            var valId = fn->AllocValue(argTypes[i], entryBlock, -1);
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
            fn->AppendToBlock(entryBlock, instrId);
            fn->Values[valId].DefInstrIndex = instrId;
        }

        // Walk bytecode
        var ip = code;
        var end = code + codeSize;
        var currentBlock = entryBlock;

        while (ip < end)
        {
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
                    var target = (int)*ip; ip++;
                    // Create branch instruction (target is label index, resolved later)
                    var instr = IRInstruction.CreateBranch(0, target);
                    var instrId = fn->AddInstruction(instr);
                    fn->AppendToBlock(currentBlock, instrId);
                    // Start a new block for fall-through (if any)
                    currentBlock = fn->AddBlock();
                    break;
                }

                case OpCodeValue.JMP_T:
                {
                    var target = (int)*ip; ip++;
                    var cond = stack[--sp];
                    var falseBlock = fn->AddBlock();
                    var instr = IRInstruction.CreateCondBranch(0, IROp.BranchTrue, cond, target, falseBlock);
                    var instrId = fn->AddInstruction(instr);
                    fn->AppendToBlock(currentBlock, instrId);
                    currentBlock = falseBlock;
                    break;
                }

                case OpCodeValue.JMP_F:
                {
                    var target = (int)*ip; ip++;
                    var cond = stack[--sp];
                    var falseBlock = fn->AddBlock();
                    var instr = IRInstruction.CreateCondBranch(0, IROp.BranchFalse, cond, target, falseBlock);
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

                default:
                    // Unhandled opcode — emit Nop and skip operands
                    // TODO: handle remaining opcodes as the JIT matures
                    break;
            }
        }

        return fn;
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
