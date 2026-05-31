namespace ishtar.jit;

using Iced.Intel;
using static Iced.Intel.AssemblerRegisters;

/// <summary>
/// x64 code generator: takes an optimized IRFunction and emits native machine code via Iced.
/// Produces a callable function pointer with signature matching the Vein calling convention.
///
/// Generated code follows: void compiled(stackval* args, stackval* result)
/// Same trampoline convention as NativeCallMarshaller for uniformity.
/// </summary>
public static unsafe class X64CodeGenerator
{
    /// <summary>
    /// Compile an IRFunction into executable native code.
    /// Returns a function pointer: void(stackval* args, stackval* result)
    /// </summary>
    public static void* Compile(IRFunction* fn) => Compile(fn, out _);

    /// <summary>
    /// Compile and also return the raw machine code bytes for disassembly.
    /// </summary>
    public static void* Compile(IRFunction* fn, out byte[] machineCode)
    {
        // 1. Register allocation
        var regAlloc = RegisterAllocator.Allocate(fn);

        // 2. Emit code
        var asm = new Assembler(64);
        EmitPrologue(asm, &regAlloc);
        EmitBody(asm, fn, &regAlloc);
        // Epilogue is emitted per-return in EmitBody

        // 3. Assemble
        using var stream = new MemoryStream();
        asm.Assemble(new StreamCodeWriter(stream), 0);
        machineCode = stream.ToArray();

        return ExecutableMemory.Alloc(machineCode);
    }

    /// <summary>
    /// Compute total RSP adjustment after all pushes: spill frame + alignment padding.
    /// Ensures RSP is 16-byte aligned before any call instruction.
    /// Entry RSP%16==8 (return address pushed by caller). After (4 + N) pushes where
    /// 4 = rbp/r13/r14/r15 and N = callee-saved from mask, RSP is aligned only if (4+N) is odd.
    /// When (4+N) is even (i.e. N is even), we need 8 extra bytes of padding.
    /// </summary>
    private static int ComputeFrameAdjustment(RegisterAllocator.AllocResult* alloc)
    {
        var calleeSavedCount = 0;
        for (var i = RegisterAllocator.GetCalleeSavedStart(); i < RegisterAllocator.GetTotalIntRegs(); i++)
        {
            if ((alloc->CalleeSavedMask & (1 << i)) != 0)
                calleeSavedCount++;
        }
        // 4 fixed pushes + N callee-saved; need odd total for 16-byte alignment
        var alignPad = (calleeSavedCount % 2 == 0) ? 8 : 0;
        return alloc->SpillFrameSize + alignPad;
    }

    private static void EmitPrologue(Assembler asm, RegisterAllocator.AllocResult* alloc)
    {
        asm.push(rbp);
        asm.mov(rbp, rsp);

        // Always save r13/r14/r15 — we unconditionally overwrite them with args/result/frame pointers
        asm.push(r13);
        asm.push(r14);
        asm.push(r15);

        // Push callee-saved registers
        for (var i = RegisterAllocator.GetCalleeSavedStart(); i < RegisterAllocator.GetTotalIntRegs(); i++)
        {
            if ((alloc->CalleeSavedMask & (1 << i)) != 0)
                asm.push(RegisterAllocator.GetIntReg(i));
        }

        // Allocate spill frame + alignment padding (ensures 16-byte RSP alignment for calls)
        var frameAdj = ComputeFrameAdjustment(alloc);
        if (frameAdj > 0)
            asm.sub(rsp, frameAdj);

        // Save input pointers:
        // On Windows: RCX = args, RDX = result, R8 = frame
        // On SysV:    RDI = args, RSI = result, RDX = frame
        // We use R14 = args, R15 = result, R13 = frame (all callee-saved)
        if (OperatingSystem.IsWindows())
        {
            asm.mov(r14, rcx);  // args
            asm.mov(r15, rdx);  // result
            asm.mov(r13, r8);   // frame
        }
        else
        {
            asm.mov(r14, rdi);  // args
            asm.mov(r15, rsi);  // result
            asm.mov(r13, rdx);  // frame
        }
    }

    private static void EmitEpilogue(Assembler asm, RegisterAllocator.AllocResult* alloc)
    {
        var frameAdj = ComputeFrameAdjustment(alloc);
        if (frameAdj > 0)
            asm.add(rsp, frameAdj);

        // Pop callee-saved registers (reverse order)
        for (var i = RegisterAllocator.GetTotalIntRegs() - 1; i >= RegisterAllocator.GetCalleeSavedStart(); i--)
        {
            if ((alloc->CalleeSavedMask & (1 << i)) != 0)
                asm.pop(RegisterAllocator.GetIntReg(i));
        }

        // Restore r13/r14/r15 (always saved in prologue)
        asm.pop(r15);
        asm.pop(r14);
        asm.pop(r13);

        asm.pop(rbp);
        asm.ret();
    }

    private static void EmitBody(Assembler asm, IRFunction* fn, RegisterAllocator.AllocResult* alloc)
    {
        // Create labels for each block
        var labels = new Label[fn->BlockCount];
        for (var i = 0; i < fn->BlockCount; i++)
            labels[i] = asm.CreateLabel();

        // Emit blocks in order
        for (var b = 0; b < fn->BlockCount; b++)
        {
            var block = &fn->Blocks[b];
            var instrCount = block->Instructions->Count;

            // Skip empty blocks (e.g. dead block after return)
            if (instrCount == 0)
            {
                // Iced requires at least one instruction after a label
                asm.Label(ref labels[b]);
                asm.nop();
                continue;
            }

            asm.Label(ref labels[b]);

            for (var i = 0; i < instrCount; i++)
            {
                var instrId = block->Instructions->Get(i)->Id;
                var instr = &fn->Instructions[instrId];
                if (instr->IsDead) continue;

                EmitInstruction(asm, fn, instr, alloc, labels);
            }
        }
    }

    private static void EmitInstruction(Assembler asm, IRFunction* fn, IRInstruction* instr,
        RegisterAllocator.AllocResult* alloc, Label[] labels)
    {
        switch (instr->Op)
        {
            case IROp.Const:
                EmitConst(asm, fn, instr, alloc);
                break;

            case IROp.Add:
            case IROp.Sub:
            case IROp.Mul:
            case IROp.And:
            case IROp.Or:
            case IROp.Xor:
                EmitBinaryArith(asm, fn, instr, alloc);
                break;

            case IROp.Div:
            case IROp.Mod:
                EmitDivMod(asm, fn, instr, alloc);
                break;

            case IROp.Shl:
            case IROp.Shr:
                EmitShift(asm, fn, instr, alloc);
                break;

            case IROp.Neg:
                EmitNeg(asm, fn, instr, alloc);
                break;

            case IROp.CmpEq:
            case IROp.CmpNe:
            case IROp.CmpLt:
            case IROp.CmpLe:
            case IROp.CmpGt:
            case IROp.CmpGe:
                EmitCompare(asm, fn, instr, alloc);
                break;

            case IROp.LoadArg:
                EmitLoadArg(asm, fn, instr, alloc);
                break;

            case IROp.StoreArg:
                EmitStoreArg(asm, fn, instr, alloc);
                break;

            case IROp.Return:
                EmitReturn(asm, fn, instr, alloc);
                break;

            case IROp.Branch:
                asm.jmp(labels[instr->BranchTarget0]);
                break;

            case IROp.BranchTrue:
                EmitCondBranch(asm, fn, instr, alloc, labels, true);
                break;

            case IROp.BranchFalse:
                EmitCondBranch(asm, fn, instr, alloc, labels, false);
                break;

            case IROp.Nop:
                break;

            case IROp.Call:
                EmitCall(asm, fn, instr, alloc);
                break;

            case IROp.CallIndirect:
                EmitCallIndirect(asm, fn, instr, alloc);
                break;

            case IROp.InitStruct:
                EmitInitStruct(asm, fn, instr, alloc);
                break;

            case IROp.CopyStruct:
                EmitCopyStruct(asm, fn, instr, alloc);
                break;

            case IROp.StoreField:
                EmitStoreField(asm, fn, instr, alloc);
                break;

            case IROp.LoadField:
                EmitLoadField(asm, fn, instr, alloc);
                break;

            case IROp.Box:
                EmitBox(asm, fn, instr, alloc);
                break;

            case IROp.Unbox:
                EmitUnbox(asm, fn, instr, alloc);
                break;
        }
    }

    // ═══════════════════════════════════════════════════════════════════
    // Individual instruction emitters
    // ═══════════════════════════════════════════════════════════════════

    private static void EmitConst(Assembler asm, IRFunction* fn, IRInstruction* instr,
        RegisterAllocator.AllocResult* alloc)
    {
        var dst = ResolveDestGPR(asm, alloc, instr->ResultId);
        var type = fn->Values[instr->ResultId].Type;

        if (IRTypeMap.IsFloat(type))
        {
            // Float const: mov to GPR then movq to XMM
            var dstXmm = ResolveDestXMM(alloc, instr->ResultId);
            asm.mov(rax, instr->Immediate);
            asm.movq(dstXmm, rax);
        }
        else
        {
            asm.mov(dst, instr->Immediate);
        }
    }

    private static void EmitBinaryArith(Assembler asm, IRFunction* fn, IRInstruction* instr,
        RegisterAllocator.AllocResult* alloc)
    {
        var resultType = fn->Values[instr->ResultId].Type;

        if (IRTypeMap.IsFloat(resultType))
        {
            EmitBinaryFloat(asm, fn, instr, alloc);
            return;
        }

        var dst = ResolveDestGPR(asm, alloc, instr->ResultId);
        var lhs = LoadOperandGPR(asm, fn, alloc, instr->GetOperand(0), rax);
        var rhs = LoadOperandGPR(asm, fn, alloc, instr->GetOperand(1), rcx);

        // Handle register conflicts:
        // If dst == rhs, moving lhs into dst would clobber rhs.
        if (dst == rhs && dst != lhs)
        {
            // For commutative ops, swap lhs/rhs
            if (instr->Op is IROp.Add or IROp.Mul or IROp.And or IROp.Or or IROp.Xor)
            {
                (lhs, rhs) = (rhs, lhs);
                // Now dst == lhs, no mov needed, rhs is safe
            }
            else
            {
                // Non-commutative (sub): use scratch to save rhs
                asm.mov(r11, rhs);
                rhs = r11;
                asm.mov(dst, lhs);
            }
        }
        else if (dst != lhs)
        {
            asm.mov(dst, lhs);
        }

        switch (instr->Op)
        {
            case IROp.Add: asm.add(dst, rhs); break;
            case IROp.Sub: asm.sub(dst, rhs); break;
            case IROp.Mul: asm.imul(dst, rhs); break;
            case IROp.And: asm.and(dst, rhs); break;
            case IROp.Or:  asm.or(dst, rhs); break;
            case IROp.Xor: asm.xor(dst, rhs); break;
        }
    }

    private static void EmitBinaryFloat(Assembler asm, IRFunction* fn, IRInstruction* instr,
        RegisterAllocator.AllocResult* alloc)
    {
        var resultType = fn->Values[instr->ResultId].Type;
        var dst = ResolveDestXMM(alloc, instr->ResultId);
        var lhs = LoadOperandXMM(asm, fn, alloc, instr->GetOperand(0), xmm14);
        var rhs = LoadOperandXMM(asm, fn, alloc, instr->GetOperand(1), xmm15);

        if (dst != lhs)
        {
            if (resultType == IRType.R4) asm.movss(dst, lhs);
            else asm.movsd(dst, lhs);
        }

        if (resultType == IRType.R4)
        {
            switch (instr->Op)
            {
                case IROp.Add: asm.addss(dst, rhs); break;
                case IROp.Sub: asm.subss(dst, rhs); break;
                case IROp.Mul: asm.mulss(dst, rhs); break;
            }
        }
        else
        {
            switch (instr->Op)
            {
                case IROp.Add: asm.addsd(dst, rhs); break;
                case IROp.Sub: asm.subsd(dst, rhs); break;
                case IROp.Mul: asm.mulsd(dst, rhs); break;
            }
        }
    }

    private static void EmitDivMod(Assembler asm, IRFunction* fn, IRInstruction* instr,
        RegisterAllocator.AllocResult* alloc)
    {
        // x64 idiv: RDX:RAX / operand → quotient in RAX, remainder in RDX
        var lhs = LoadOperandGPR(asm, fn, alloc, instr->GetOperand(0), rax);
        var rhs = LoadOperandGPR(asm, fn, alloc, instr->GetOperand(1), rcx);

        // rhs must NOT be rax or rdx (clobbered by cqo/idiv setup)
        if (rhs == rax || rhs == rdx)
        {
            asm.mov(r11, rhs);
            rhs = r11;
        }

        if (lhs != rax) asm.mov(rax, lhs);
        asm.cqo(); // sign-extend RAX → RDX:RAX
        asm.idiv(rhs);

        var dst = ResolveDestGPR(asm, alloc, instr->ResultId);
        if (instr->Op == IROp.Div)
        {
            if (dst != rax) asm.mov(dst, rax);
        }
        else // Mod
        {
            if (dst != rdx) asm.mov(dst, rdx);
        }
    }

    private static void EmitShift(Assembler asm, IRFunction* fn, IRInstruction* instr,
        RegisterAllocator.AllocResult* alloc)
    {
        var dst = ResolveDestGPR(asm, alloc, instr->ResultId);
        var lhs = LoadOperandGPR(asm, fn, alloc, instr->GetOperand(0), rax);
        var rhs = LoadOperandGPR(asm, fn, alloc, instr->GetOperand(1), rcx);

        if (dst != lhs) asm.mov(dst, lhs);
        if (rhs != rcx) asm.mov(rcx, rhs);

        if (instr->Op == IROp.Shl)
            asm.shl(dst, cl);
        else
            asm.sar(dst, cl);
    }

    private static void EmitNeg(Assembler asm, IRFunction* fn, IRInstruction* instr,
        RegisterAllocator.AllocResult* alloc)
    {
        var dst = ResolveDestGPR(asm, alloc, instr->ResultId);
        var src = LoadOperandGPR(asm, fn, alloc, instr->GetOperand(0), rax);
        if (dst != src) asm.mov(dst, src);
        asm.neg(dst);
    }

    private static void EmitCompare(Assembler asm, IRFunction* fn, IRInstruction* instr,
        RegisterAllocator.AllocResult* alloc)
    {
        var lhs = LoadOperandGPR(asm, fn, alloc, instr->GetOperand(0), rax);
        var rhs = LoadOperandGPR(asm, fn, alloc, instr->GetOperand(1), rcx);
        var dst = ResolveDestGPR(asm, alloc, instr->ResultId);

        // If dst overlaps lhs or rhs, we must save them before zeroing dst
        if (dst == lhs || dst == rhs)
        {
            var cmpL = lhs;
            var cmpR = rhs;
            if (dst == lhs) { asm.mov(r10, lhs); cmpL = r10; }
            if (dst == rhs) { asm.mov(r11, rhs); cmpR = r11; }
            asm.xor(dst, dst);
            asm.cmp(cmpL, cmpR);
        }
        else
        {
            asm.xor(dst, dst);
            asm.cmp(lhs, rhs);
        }

        // Use the low byte of dst for setCC — avoids clobbering rax when dst != rax
        var dstByte = GetLowByteReg(dst);
        switch (instr->Op)
        {
            case IROp.CmpEq: asm.sete(dstByte); break;
            case IROp.CmpNe: asm.setne(dstByte); break;
            case IROp.CmpLt: asm.setl(dstByte); break;
            case IROp.CmpLe: asm.setle(dstByte); break;
            case IROp.CmpGt: asm.setg(dstByte); break;
            case IROp.CmpGe: asm.setge(dstByte); break;
        }
        // No movzx needed — xor already zeroed upper bytes of dst
    }

    private static AssemblerRegister8 GetLowByteReg(AssemblerRegister64 reg)
    {
        if (reg == rax) return al;
        if (reg == rcx) return cl;
        if (reg == rdx) return dl;
        if (reg == rbx) return bl;
        if (reg == r8)  return r8b;
        if (reg == r9)  return r9b;
        if (reg == r10) return r10b;
        if (reg == r11) return r11b;
        if (reg == r12) return r12b;
        if (reg == r13) return r13b;
        return al; // fallback
    }

    private static void EmitLoadArg(Assembler asm, IRFunction* fn, IRInstruction* instr,
        RegisterAllocator.AllocResult* alloc)
    {
        // Load from args array: r14[argIndex * sizeof(stackval)]
        var argIdx = (int)instr->Immediate;
        var offset = argIdx * sizeof(stackval); // data is at offset 0

        var resultType = fn->Values[instr->ResultId].Type;

        if (IRTypeMap.IsFloat(resultType))
        {
            var dst = ResolveDestXMM(alloc, instr->ResultId);
            if (resultType == IRType.R4)
                asm.movss(dst, __dword_ptr[r14 + offset]);
            else
                asm.movsd(dst, __qword_ptr[r14 + offset]);
        }
        else
        {
            var dst = ResolveDestGPR(asm, alloc, instr->ResultId);
            asm.mov(dst, __qword_ptr[r14 + offset]);
        }
    }

    private static void EmitStoreArg(Assembler asm, IRFunction* fn, IRInstruction* instr,
        RegisterAllocator.AllocResult* alloc)
    {
        // Write to args array: r14[argIndex * sizeof(stackval)] = operand
        var argIdx = (int)instr->Immediate;
        var offset = argIdx * sizeof(stackval);

        var srcId = instr->GetOperand(0);
        var srcType = fn->Values[srcId].Type;

        if (IRTypeMap.IsFloat(srcType))
        {
            var src = LoadOperandXMM(asm, fn, alloc, srcId, xmm0);
            if (srcType == IRType.R4)
                asm.movss(__dword_ptr[r14 + offset], src);
            else
                asm.movsd(__qword_ptr[r14 + offset], src);
        }
        else
        {
            var src = LoadOperandGPR(asm, fn, alloc, srcId, rax);
            asm.mov(__qword_ptr[r14 + offset], src);
        }
    }

    private static void EmitReturn(Assembler asm, IRFunction* fn, IRInstruction* instr,
        RegisterAllocator.AllocResult* alloc)
    {
        // Store return value into result stackval (r15)
        if (instr->OperandCount > 0)
        {
            var retValId = instr->GetOperand(0);
            var retType = fn->Values[retValId].Type;

            if (IRTypeMap.IsFloat(retType))
            {
                var src = LoadOperandXMM(asm, fn, alloc, retValId, xmm0);
                if (retType == IRType.R4)
                    asm.movss(__dword_ptr[r15], src);
                else
                    asm.movsd(__qword_ptr[r15], src);
            }
            else
            {
                var src = LoadOperandGPR(asm, fn, alloc, retValId, rax);
                asm.mov(__qword_ptr[r15], src);
            }

            // Write type code
            var typeCodeOffset = sizeof(stack_union); // type field offset in stackval
            asm.mov(__dword_ptr[r15 + typeCodeOffset], (int)MapIRTypeToVein(retType));
        }

        // Emit epilogue inline (each return block gets its own)
        // We can't reference alloc in a Label-based epilogue, so emit inline
        var frameAdj = ComputeFrameAdjustment(alloc);
        if (frameAdj > 0)
            asm.add(rsp, frameAdj);

        for (var i = 11; i >= 7; i--) // callee-saved indices in reverse
        {
            if ((alloc->CalleeSavedMask & (1 << i)) != 0)
                asm.pop(RegisterAllocator.GetIntReg(i));
        }

        // Restore r13/r14/r15 (always saved in prologue)
        asm.pop(r15);
        asm.pop(r14);
        asm.pop(r13);

        asm.pop(rbp);
        asm.ret();
    }

    private static void EmitCondBranch(Assembler asm, IRFunction* fn, IRInstruction* instr,
        RegisterAllocator.AllocResult* alloc, Label[] labels, bool branchOnTrue)
    {
        var condId = instr->GetOperand(0);
        var cond = LoadOperandGPR(asm, fn, alloc, condId, rax);

        asm.test(cond, cond);

        if (branchOnTrue)
        {
            asm.jnz(labels[instr->BranchTarget0]);
            if (instr->BranchTarget1 >= 0)
                asm.jmp(labels[instr->BranchTarget1]);
        }
        else
        {
            asm.jz(labels[instr->BranchTarget0]);
            if (instr->BranchTarget1 >= 0)
                asm.jmp(labels[instr->BranchTarget1]);
        }
    }

    private static void EmitCall(Assembler asm, IRFunction* fn, IRInstruction* instr,
        RegisterAllocator.AllocResult* alloc)
    {
        var argCount = (int)instr->Immediate;
        // stackval layout: 8 bytes data (used portion), 16 bytes total union, 4 bytes type, 4 padding = 24
        const int stackValSize = 24; // sizeof(stackval)
        const int typeFieldOffset = 16; // offsetof(stackval, type) = sizeof(stack_union)

        // Allocate space on stack for: args array + result stackval
        // Total = (argCount + 1) * stackValSize, aligned to 16
        var totalSize = (argCount + 1) * stackValSize;
        totalSize = (totalSize + 15) & ~15; // align to 16

        asm.sub(rsp, totalSize);

        // Fill args array: [rsp + 0] .. [rsp + (argCount-1)*stackValSize]
        for (var i = 0; i < argCount; i++)
        {
            var argValId = instr->GetOperand(i);
            var argType = fn->Values[argValId].Type;
            var argOffset = i * stackValSize;

            if (IRTypeMap.IsFloat(argType))
            {
                var src = LoadOperandXMM(asm, fn, alloc, argValId, xmm0);
                if (argType == IRType.R4)
                    asm.movss(__dword_ptr[rsp + argOffset], src);
                else
                    asm.movsd(__qword_ptr[rsp + argOffset], src);
            }
            else
            {
                var src = LoadOperandGPR(asm, fn, alloc, argValId, rax);
                asm.mov(__qword_ptr[rsp + argOffset], src);
            }

            // Write type tag
            asm.mov(__dword_ptr[rsp + argOffset + typeFieldOffset], (int)MapIRTypeToVein(argType));
        }

        // Result slot is at [rsp + argCount * stackValSize]
        var resultOffset = argCount * stackValSize;

        // Set up calling convention: void(stackval* args, stackval* result, CallFrame* frame)
        if (OperatingSystem.IsWindows())
        {
            asm.lea(rcx, __[rsp]);                      // args
            asm.lea(rdx, __[rsp + resultOffset]);       // result
            asm.mov(r8, r13);                           // frame
        }
        else
        {
            asm.lea(rdi, __[rsp]);                      // args
            asm.lea(rsi, __[rsp + resultOffset]);       // result
            asm.mov(rdx, r13);                          // frame
        }

        // Call target (MethodRef stores the native function pointer)
        asm.mov(rax, instr->MethodRef);
        asm.call(rax);

        // Extract result
        if (instr->ResultId >= 0)
        {
            var resultType = fn->Values[instr->ResultId].Type;
            if (IRTypeMap.IsFloat(resultType))
            {
                var dst = ResolveDestXMM(alloc, instr->ResultId);
                if (resultType == IRType.R4)
                    asm.movss(dst, __dword_ptr[rsp + resultOffset]);
                else
                    asm.movsd(dst, __qword_ptr[rsp + resultOffset]);
            }
            else
            {
                var dst = ResolveDestGPR(asm, alloc, instr->ResultId);
                asm.mov(dst, __qword_ptr[rsp + resultOffset]);
            }
        }

        // Restore stack
        asm.add(rsp, totalSize);
    }

    /// <summary>
    /// Emit an indirect call: MethodRef holds the ADDRESS of the function pointer (e.g. &amp;method-&gt;PIInfo.compiled_func_ref).
    /// Used for self-recursive calls where the pointer is filled after compilation.
    /// </summary>
    private static void EmitCallIndirect(Assembler asm, IRFunction* fn, IRInstruction* instr,
        RegisterAllocator.AllocResult* alloc)
    {
        var argCount = (int)instr->Immediate;
        const int stackValSize = 24;
        const int typeFieldOffset = 16;

        var totalSize = (argCount + 1) * stackValSize;
        totalSize = (totalSize + 15) & ~15;

        asm.sub(rsp, totalSize);

        // Fill args
        for (var i = 0; i < argCount; i++)
        {
            var argValId = instr->GetOperand(i);
            var argType = fn->Values[argValId].Type;
            var argOffset = i * stackValSize;

            if (IRTypeMap.IsFloat(argType))
            {
                var src = LoadOperandXMM(asm, fn, alloc, argValId, xmm0);
                if (argType == IRType.R4)
                    asm.movss(__dword_ptr[rsp + argOffset], src);
                else
                    asm.movsd(__qword_ptr[rsp + argOffset], src);
            }
            else
            {
                var src = LoadOperandGPR(asm, fn, alloc, argValId, rax);
                asm.mov(__qword_ptr[rsp + argOffset], src);
            }

            asm.mov(__dword_ptr[rsp + argOffset + typeFieldOffset], (int)MapIRTypeToVein(argType));
        }

        var resultOffset = argCount * stackValSize;

        // Set up calling convention
        if (OperatingSystem.IsWindows())
        {
            asm.lea(rcx, __[rsp]);
            asm.lea(rdx, __[rsp + resultOffset]);
            asm.mov(r8, r13);   // frame
        }
        else
        {
            asm.lea(rdi, __[rsp]);
            asm.lea(rsi, __[rsp + resultOffset]);
            asm.mov(rdx, r13);  // frame
        }

        // Indirect call: MethodRef = address of the function pointer slot
        // mov rax, &slot; mov rax, [rax]; call rax
        asm.mov(rax, instr->MethodRef);
        asm.mov(rax, __qword_ptr[rax]);
        asm.call(rax);

        // Extract result
        if (instr->ResultId >= 0)
        {
            var resultType = fn->Values[instr->ResultId].Type;
            if (IRTypeMap.IsFloat(resultType))
            {
                var dst = ResolveDestXMM(alloc, instr->ResultId);
                if (resultType == IRType.R4)
                    asm.movss(dst, __dword_ptr[rsp + resultOffset]);
                else
                    asm.movsd(dst, __qword_ptr[rsp + resultOffset]);
            }
            else
            {
                var dst = ResolveDestGPR(asm, alloc, instr->ResultId);
                asm.mov(dst, __qword_ptr[rsp + resultOffset]);
            }
        }

        asm.add(rsp, totalSize);
    }

    // ═══════════════════════════════════════════════════════════════════
    // Struct operation emitters (call into JitHelpers via R13=frame)
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// INITSTRUCT: call Helper_InitStruct(RuntimeIshtarClass* clazz, CallFrame* frame) → IshtarObject*
    /// MethodRef = class pointer, result = allocated object pointer
    /// </summary>
    private static void EmitInitStruct(Assembler asm, IRFunction* fn, IRInstruction* instr,
        RegisterAllocator.AllocResult* alloc)
    {
        // Align stack for call (16-byte alignment)
        asm.sub(rsp, 32); // shadow space (Windows) / alignment

        if (OperatingSystem.IsWindows())
        {
            asm.mov(rcx, instr->MethodRef);  // arg0: class ptr
            asm.mov(rdx, r13);               // arg1: frame
        }
        else
        {
            asm.mov(rdi, instr->MethodRef);  // arg0: class ptr
            asm.mov(rsi, r13);               // arg1: frame
        }

        asm.mov(rax, JitHelpers.Table.InitStruct);
        asm.call(rax);

        asm.add(rsp, 32);

        // Result in RAX → move to destination register
        var dst = ResolveDestGPR(asm, alloc, instr->ResultId);
        if (dst != rax) asm.mov(dst, rax);
    }

    /// <summary>
    /// CPSTRUCT: call Helper_CopyStruct(IshtarObject* src, RuntimeIshtarClass* clazz, CallFrame* frame) → IshtarObject*
    /// MethodRef = class pointer, operand[0] = source object
    /// </summary>
    private static void EmitCopyStruct(Assembler asm, IRFunction* fn, IRInstruction* instr,
        RegisterAllocator.AllocResult* alloc)
    {
        var src = LoadOperandGPR(asm, fn, alloc, instr->GetOperand(0), r11);

        asm.sub(rsp, 32);

        if (OperatingSystem.IsWindows())
        {
            asm.mov(rcx, src);               // arg0: source obj
            asm.mov(rdx, instr->MethodRef);  // arg1: class ptr
            asm.mov(r8, r13);                // arg2: frame
        }
        else
        {
            asm.mov(rdi, src);               // arg0: source obj
            asm.mov(rsi, instr->MethodRef);  // arg1: class ptr
            asm.mov(rdx, r13);               // arg2: frame
        }

        asm.mov(rax, JitHelpers.Table.CopyStruct);
        asm.call(rax);

        asm.add(rsp, 32);

        var dst = ResolveDestGPR(asm, alloc, instr->ResultId);
        if (dst != rax) asm.mov(dst, rax);
    }

    /// <summary>
    /// STF: call Helper_StoreField(IshtarObject* obj, RuntimeIshtarField* field, stackval* value, CallFrame* frame)
    /// operand[0] = value (written to temp stackval on stack), operand[1] = object pointer
    /// MethodRef = field pointer, Immediate = value's VeinTypeCode
    /// </summary>
    private static void EmitStoreField(Assembler asm, IRFunction* fn, IRInstruction* instr,
        RegisterAllocator.AllocResult* alloc)
    {
        const int stackValSize = 24;
        const int typeFieldOffset = 16;

        var valueId = instr->GetOperand(0);
        var objId = instr->GetOperand(1);
        var valueType = fn->Values[valueId].Type;

        // Allocate temp stackval on stack + shadow space
        var frameSize = stackValSize + 32;
        if ((frameSize % 16) != 0) frameSize += 8;
        asm.sub(rsp, frameSize);

        // Write value into temp stackval at [rsp + 32]
        var svOffset = 32;
        if (IRTypeMap.IsFloat(valueType))
        {
            var src = LoadOperandXMM(asm, fn, alloc, valueId, xmm0);
            if (valueType == IRType.R4)
                asm.movss(__dword_ptr[rsp + svOffset], src);
            else
                asm.movsd(__qword_ptr[rsp + svOffset], src);
        }
        else
        {
            var src = LoadOperandGPR(asm, fn, alloc, valueId, rax);
            asm.mov(__qword_ptr[rsp + svOffset], src);
        }
        asm.mov(__dword_ptr[rsp + svOffset + typeFieldOffset], (int)MapIRTypeToVein(valueType));

        var obj = LoadOperandGPR(asm, fn, alloc, objId, r11);

        if (OperatingSystem.IsWindows())
        {
            asm.mov(rcx, obj);                       // arg0: object
            asm.mov(rdx, instr->MethodRef);          // arg1: field ptr
            asm.lea(r8, __[rsp + svOffset]);         // arg2: &stackval
            asm.mov(r9, r13);                        // arg3: frame
        }
        else
        {
            asm.mov(rdi, obj);                       // arg0: object
            asm.mov(rsi, instr->MethodRef);          // arg1: field ptr
            asm.lea(rdx, __[rsp + svOffset]);        // arg2: &stackval
            asm.mov(rcx, r13);                       // arg3: frame
        }

        asm.mov(rax, JitHelpers.Table.StoreField);
        asm.call(rax);

        asm.add(rsp, frameSize);
    }

    /// <summary>
    /// LDF: call Helper_LoadField(IshtarObject* obj, RuntimeIshtarField* field, CallFrame* frame, stackval* result)
    /// operand[0] = object pointer, MethodRef = field pointer
    /// Result loaded from the output stackval's data field.
    /// </summary>
    private static void EmitLoadField(Assembler asm, IRFunction* fn, IRInstruction* instr,
        RegisterAllocator.AllocResult* alloc)
    {
        const int stackValSize = 24;

        var objId = instr->GetOperand(0);
        var resultType = fn->Values[instr->ResultId].Type;

        // Allocate output stackval on stack + shadow space
        var frameSize = stackValSize + 32;
        if ((frameSize % 16) != 0) frameSize += 8;
        asm.sub(rsp, frameSize);

        var svOffset = 32;
        var obj = LoadOperandGPR(asm, fn, alloc, objId, r11);

        if (OperatingSystem.IsWindows())
        {
            asm.mov(rcx, obj);                       // arg0: object
            asm.mov(rdx, instr->MethodRef);          // arg1: field ptr
            asm.mov(r8, r13);                        // arg2: frame
            asm.lea(r9, __[rsp + svOffset]);         // arg3: &result stackval
        }
        else
        {
            asm.mov(rdi, obj);                       // arg0: object
            asm.mov(rsi, instr->MethodRef);          // arg1: field ptr
            asm.mov(rdx, r13);                       // arg2: frame
            asm.lea(rcx, __[rsp + svOffset]);        // arg3: &result stackval
        }

        asm.mov(rax, JitHelpers.Table.LoadField);
        asm.call(rax);

        // Load result from stackval at [rsp + svOffset]
        if (IRTypeMap.IsFloat(resultType))
        {
            var dst = ResolveDestXMM(alloc, instr->ResultId);
            if (resultType == IRType.R4)
                asm.movss(dst, __dword_ptr[rsp + svOffset]);
            else
                asm.movsd(dst, __qword_ptr[rsp + svOffset]);
        }
        else
        {
            var dst = ResolveDestGPR(asm, alloc, instr->ResultId);
            asm.mov(dst, __qword_ptr[rsp + svOffset]);
        }

        asm.add(rsp, frameSize);
    }

    /// <summary>
    /// BOX: call Helper_Box(stackval* value, RuntimeIshtarClass* clazz, CallFrame* frame) → IshtarObject*
    /// operand[0] = value, MethodRef = class pointer
    /// </summary>
    private static void EmitBox(Assembler asm, IRFunction* fn, IRInstruction* instr,
        RegisterAllocator.AllocResult* alloc)
    {
        const int stackValSize = 24;
        const int typeFieldOffset = 16;

        var valueId = instr->GetOperand(0);
        var valueType = fn->Values[valueId].Type;

        var frameSize = stackValSize + 32;
        if ((frameSize % 16) != 0) frameSize += 8;
        asm.sub(rsp, frameSize);

        var svOffset = 32;
        if (IRTypeMap.IsFloat(valueType))
        {
            var src = LoadOperandXMM(asm, fn, alloc, valueId, xmm0);
            if (valueType == IRType.R4)
                asm.movss(__dword_ptr[rsp + svOffset], src);
            else
                asm.movsd(__qword_ptr[rsp + svOffset], src);
        }
        else
        {
            var src = LoadOperandGPR(asm, fn, alloc, valueId, rax);
            asm.mov(__qword_ptr[rsp + svOffset], src);
        }
        asm.mov(__dword_ptr[rsp + svOffset + typeFieldOffset], (int)MapIRTypeToVein(valueType));

        if (OperatingSystem.IsWindows())
        {
            asm.lea(rcx, __[rsp + svOffset]);    // arg0: &stackval
            asm.mov(rdx, instr->MethodRef);      // arg1: class ptr
            asm.mov(r8, r13);                    // arg2: frame
        }
        else
        {
            asm.lea(rdi, __[rsp + svOffset]);    // arg0: &stackval
            asm.mov(rsi, instr->MethodRef);      // arg1: class ptr
            asm.mov(rdx, r13);                   // arg2: frame
        }

        asm.mov(rax, JitHelpers.Table.Box);
        asm.call(rax);

        asm.add(rsp, frameSize);

        var dst = ResolveDestGPR(asm, alloc, instr->ResultId);
        if (dst != rax) asm.mov(dst, rax);
    }

    /// <summary>
    /// UNBOX: call Helper_Unbox(IshtarObject* obj, RuntimeIshtarClass* clazz, CallFrame* frame, stackval* result)
    /// operand[0] = object, MethodRef = class pointer
    /// </summary>
    private static void EmitUnbox(Assembler asm, IRFunction* fn, IRInstruction* instr,
        RegisterAllocator.AllocResult* alloc)
    {
        const int stackValSize = 24;

        var objId = instr->GetOperand(0);
        var resultType = fn->Values[instr->ResultId].Type;

        var frameSize = stackValSize + 32;
        if ((frameSize % 16) != 0) frameSize += 8;
        asm.sub(rsp, frameSize);

        var svOffset = 32;
        var obj = LoadOperandGPR(asm, fn, alloc, objId, r11);

        if (OperatingSystem.IsWindows())
        {
            asm.mov(rcx, obj);                       // arg0: object
            asm.mov(rdx, instr->MethodRef);          // arg1: class ptr
            asm.mov(r8, r13);                        // arg2: frame
            asm.lea(r9, __[rsp + svOffset]);         // arg3: &result stackval
        }
        else
        {
            asm.mov(rdi, obj);                       // arg0: object
            asm.mov(rsi, instr->MethodRef);          // arg1: class ptr
            asm.mov(rdx, r13);                       // arg2: frame
            asm.lea(rcx, __[rsp + svOffset]);        // arg3: &result stackval
        }

        asm.mov(rax, JitHelpers.Table.Unbox);
        asm.call(rax);

        // Load result
        if (IRTypeMap.IsFloat(resultType))
        {
            var dst = ResolveDestXMM(alloc, instr->ResultId);
            if (resultType == IRType.R4)
                asm.movss(dst, __dword_ptr[rsp + svOffset]);
            else
                asm.movsd(dst, __qword_ptr[rsp + svOffset]);
        }
        else
        {
            var dst = ResolveDestGPR(asm, alloc, instr->ResultId);
            asm.mov(dst, __qword_ptr[rsp + svOffset]);
        }

        asm.add(rsp, frameSize);
    }

    // ═══════════════════════════════════════════════════════════════════
    // Register resolution helpers
    // ═══════════════════════════════════════════════════════════════════

    private static AssemblerRegister64 ResolveDestGPR(Assembler asm, RegisterAllocator.AllocResult* alloc, int valueId)
    {
        var a = &alloc->Allocations[valueId];
        if (a->RegIndex >= 0 && !a->IsFloat)
            return RegisterAllocator.GetIntReg(a->RegIndex);
        // Spilled: use rax as temporary, store after
        return rax;
    }

    private static AssemblerRegisterXMM ResolveDestXMM(RegisterAllocator.AllocResult* alloc, int valueId)
    {
        var a = &alloc->Allocations[valueId];
        if (a->RegIndex >= 0 && a->IsFloat)
            return RegisterAllocator.GetFloatReg(a->RegIndex);
        return xmm0;
    }

    private static AssemblerRegister64 LoadOperandGPR(Assembler asm, IRFunction* fn,
        RegisterAllocator.AllocResult* alloc, int valueId, AssemblerRegister64 scratch)
    {
        var a = &alloc->Allocations[valueId];
        if (a->RegIndex >= 0 && !a->IsFloat)
            return RegisterAllocator.GetIntReg(a->RegIndex);

        // Spilled: load from stack
        if (a->SpillOffset != 0)
        {
            asm.mov(scratch, __qword_ptr[rbp + a->SpillOffset]);
            return scratch;
        }

        return scratch;
    }

    private static AssemblerRegisterXMM LoadOperandXMM(Assembler asm, IRFunction* fn,
        RegisterAllocator.AllocResult* alloc, int valueId, AssemblerRegisterXMM scratch)
    {
        var a = &alloc->Allocations[valueId];
        if (a->RegIndex >= 0 && a->IsFloat)
            return RegisterAllocator.GetFloatReg(a->RegIndex);

        // Spilled
        if (a->SpillOffset != 0)
        {
            asm.movsd(scratch, __qword_ptr[rbp + a->SpillOffset]);
            return scratch;
        }

        return scratch;
    }

    private static vein.runtime.VeinTypeCode MapIRTypeToVein(IRType type) => type switch
    {
        IRType.I1 => vein.runtime.VeinTypeCode.TYPE_I1,
        IRType.I2 => vein.runtime.VeinTypeCode.TYPE_I2,
        IRType.I4 => vein.runtime.VeinTypeCode.TYPE_I4,
        IRType.I8 => vein.runtime.VeinTypeCode.TYPE_I8,
        IRType.U1 => vein.runtime.VeinTypeCode.TYPE_U1,
        IRType.U2 => vein.runtime.VeinTypeCode.TYPE_U2,
        IRType.U4 => vein.runtime.VeinTypeCode.TYPE_U4,
        IRType.U8 => vein.runtime.VeinTypeCode.TYPE_U8,
        IRType.R4 => vein.runtime.VeinTypeCode.TYPE_R4,
        IRType.R8 => vein.runtime.VeinTypeCode.TYPE_R8,
        IRType.Ptr => vein.runtime.VeinTypeCode.TYPE_RAW,
        IRType.Bool => vein.runtime.VeinTypeCode.TYPE_BOOLEAN,
        _ => vein.runtime.VeinTypeCode.TYPE_VOID
    };
}
