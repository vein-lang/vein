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

    private static void EmitPrologue(Assembler asm, RegisterAllocator.AllocResult* alloc)
    {
        asm.push(rbp);
        asm.mov(rbp, rsp);

        // Push callee-saved registers
        for (var i = RegisterAllocator.GetCalleeSavedStart(); i < RegisterAllocator.GetTotalIntRegs(); i++)
        {
            if ((alloc->CalleeSavedMask & (1 << i)) != 0)
                asm.push(RegisterAllocator.GetIntReg(i));
        }

        // Allocate spill frame
        var spillSize = alloc->SpillFrameSize;
        if (spillSize > 0)
            asm.sub(rsp, spillSize);

        // Save input pointers:
        // On Windows: RCX = args, RDX = result → save to callee-saved regs
        // On SysV:    RDI = args, RSI = result
        // We use R14 = args, R15 = result (callee-saved, always available)
        if (OperatingSystem.IsWindows())
        {
            asm.mov(r14, rcx);  // args
            asm.mov(r15, rdx);  // result
        }
        else
        {
            asm.mov(r14, rdi);  // args
            asm.mov(r15, rsi);  // result
        }
    }

    private static void EmitEpilogue(Assembler asm, RegisterAllocator.AllocResult* alloc)
    {
        var spillSize = alloc->SpillFrameSize;
        if (spillSize > 0)
            asm.add(rsp, spillSize);

        // Pop callee-saved registers (reverse order)
        for (var i = RegisterAllocator.GetTotalIntRegs() - 1; i >= RegisterAllocator.GetCalleeSavedStart(); i--)
        {
            if ((alloc->CalleeSavedMask & (1 << i)) != 0)
                asm.pop(RegisterAllocator.GetIntReg(i));
        }

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

        // If dst overlaps lhs or rhs, we must cmp BEFORE zeroing dst
        // But xor clobbers flags, so: save operands if needed, xor, then cmp with saved values
        if (dst == lhs || dst == rhs)
        {
            // Save operands to scratch regs if they'd be clobbered
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

        switch (instr->Op)
        {
            case IROp.CmpEq: asm.sete(al); break;
            case IROp.CmpNe: asm.setne(al); break;
            case IROp.CmpLt: asm.setl(al); break;
            case IROp.CmpLe: asm.setle(al); break;
            case IROp.CmpGt: asm.setg(al); break;
            case IROp.CmpGe: asm.setge(al); break;
        }
        // If dst is not rax, movzx the result
        if (dst != rax)
            asm.movzx(dst, al);
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
        var spillSize = alloc->SpillFrameSize;
        if (spillSize > 0)
            asm.add(rsp, spillSize);

        for (var i = 11; i >= 7; i--) // callee-saved indices in reverse
        {
            if ((alloc->CalleeSavedMask & (1 << i)) != 0)
                asm.pop(RegisterAllocator.GetIntReg(i));
        }

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
        // Indirect call via MethodRef pointer
        asm.mov(rax, instr->MethodRef);
        asm.call(rax);

        // If has result, move RAX/XMM0 to destination
        if (instr->ResultId >= 0)
        {
            var resultType = fn->Values[instr->ResultId].Type;
            if (IRTypeMap.IsFloat(resultType))
            {
                var dst = ResolveDestXMM(alloc, instr->ResultId);
                if (dst != xmm0)
                {
                    if (resultType == IRType.R4) asm.movss(dst, xmm0);
                    else asm.movsd(dst, xmm0);
                }
            }
            else
            {
                var dst = ResolveDestGPR(asm, alloc, instr->ResultId);
                if (dst != rax)
                    asm.mov(dst, rax);
            }
        }
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
