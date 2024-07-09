namespace ishtar;

using Iced.Intel;
using vein.runtime;
using static Iced.Intel.AssemblerRegisters;

public unsafe class IshtarJIT(VirtualMachine vm)
{
    public static Architecture Architecture => RuntimeInformation.ProcessArchitecture;

    // x86 is not support, but need safe apply arm32
    public Assembler AllocEmitter()
        => new Assembler(64);

    public delegate*<void> GenerateHalt()
    {
        var asm = AllocEmitter();

        asm.mov(rax, 0x80);
        asm.xor(rdi, rdi);
        asm.syscall();

        return (delegate*<void>)RecollectExecutableMemory(asm);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="procedureHandle"></param>
    /// <returns></returns>
    /// <exception cref="NotSupportedException"></exception>
    public void* WrapNativeCall(void* procedureHandle)
        => WrapNativeCall(new IntPtr(procedureHandle)).ToPointer();

    /// <summary>
    /// 
    /// </summary>
    /// <param name="procedureHandle"></param>
    /// <returns></returns>
    /// <exception cref="NotSupportedException"></exception>
    public IntPtr WrapNativeCall(IntPtr procedureHandle)
    {
        if (Architecture is Architecture.Arm64 or Architecture.Arm)
            throw new NotSupportedException("Arm32/64 not support");
        if (!Environment.Is64BitProcess)
            throw new NotSupportedException("x86 not support");
        var c = AllocEmitter();
        
        //c.test(ecx, ecx);
        //c.push((uint)procedureHandle.ToInt32());

        c.test(r11, r11);
        // register spillage
        c.push(rbp);
        c.sub(rsp, 0x20);
        c.lea(rbp, __[rsp+0x20]);
        c.mov(r11, procedureHandle.ToInt64());
        c.call(r11);
        c.add(rsp, 0x20);
        c.pop(rbp);
        c.ret();

        return new IntPtr(RecollectExecutableMemory(c));
    }
    /// <exception cref="NotSupportedException"></exception>
    public void* WrapNativeCall(void* procedureHandle, void** retMemory)
        => WrapNativeCall(new IntPtr(procedureHandle), new IntPtr(retMemory)).ToPointer();

    /// <exception cref="NotSupportedException"></exception>
    public IntPtr WrapNativeCall(IntPtr procedureHandle, IntPtr retMemory)
    {
        if (Architecture is Architecture.Arm64 or Architecture.Arm)
            throw new NotSupportedException("Arm32/64 not support");
        if (!Environment.Is64BitProcess)
            throw new NotSupportedException("x86 not support");
        var c = AllocEmitter();

        c.push(rbp);
        c.sub(rsp, 0x20);
        c.lea(rbp, __[rsp+0x20]);
        c.mov(r11, procedureHandle.ToInt64());
        c.call(r11);
        c.mov(rdx, retMemory.ToInt64());
        c.mov(__[rdx], rax);
        c.add(rsp, 0x20);
        c.pop(rbp);
        
        c.ret();

        return new IntPtr(RecollectExecutableMemory(c));
    }

    public List<x64_AssemblerStep> ConvertArgumentToDesc(IReadOnlyList<VeinArgumentRef> args)
    {
        Dictionary<VeinTypeCode, Register[]> availableRegisters = new()
        {
            { VeinTypeCode.TYPE_I1, [dl, cl, r8, r9, r12, r13] },
            { VeinTypeCode.TYPE_I2, [dx, cx, r8, r9, r12, r13] },
            { VeinTypeCode.TYPE_I4, [ecx, edx, r8d, r9d] },
            { VeinTypeCode.TYPE_I8, [r8, rcx, rdx, r9, r12, r13] },
            { VeinTypeCode.TYPE_RAW, [rcx, rdx, r8, r9, r10, r11, r12, r13] },
            { VeinTypeCode.TYPE_R4, [xmm0, xmm1, xmm2, xmm3, xmm4, xmm5] },
            { VeinTypeCode.TYPE_R8, [xmm0, xmm1, xmm2, xmm3, xmm4, xmm5] },
            //{ typeof(char), [dl, cl, r8b, r9b, r12b, r13b] },
            //{ typeof(bool), [al, dl, cl, r8b, r9b, r12b, r13b] }
        };

        List<x64_AssemblerStep> argumentInfos = [];
        int stackOffset = 16;

        for (int i = 0; i < args.Count; i++)
        {
            VeinClass type = args[i].Type;

            if (!availableRegisters.ContainsKey(type.TypeCode))
            {
                argumentInfos.Add(new x64_AssemblerStep { Instruction = x64_AssemblerStep.InstructionTarget.push, StackOffset = stackOffset });
                stackOffset += 8;
            }
            else if (type is { TypeCode: VeinTypeCode.TYPE_R2 or VeinTypeCode.TYPE_R4 or VeinTypeCode.TYPE_R8 or VeinTypeCode.TYPE_R16 })
            {
                var registers = availableRegisters[type.TypeCode];
                foreach (var reg in registers)
                {
                    if (!argumentInfos.Exists(info => info.Register == reg))
                    {
                        argumentInfos.Add(new x64_AssemblerStep
                        {
                            Instruction = x64_AssemblerStep.InstructionTarget.mov,
                            Register = reg,
                            StackOffset = -1
                        });
                        break;
                    }
                }
            }
            else
            {
                var availableRegisterByType = availableRegisters[type.TypeCode].Where(x => argumentInfos.All(z => z.Register != x)).ToList();

                foreach (var reg in availableRegisterByType.Where(reg => !argumentInfos.Exists(info => info.Register == reg)))
                {
                    argumentInfos.Add(new x64_AssemblerStep { Instruction = x64_AssemblerStep.InstructionTarget.mov, Register = reg });
                    break;
                }

                if (availableRegisterByType.Any())
                    continue;

                argumentInfos.Add(new x64_AssemblerStep { Instruction = x64_AssemblerStep.InstructionTarget.push, StackOffset = stackOffset });
                stackOffset += 8;
            }
        }

        return argumentInfos;
    }

    public void GenerateHeader(Assembler asm, int stackSize, nint procedure)
    {
        asm.push(rbp);
        asm.mov(rbp, rsp);
        asm.sub(rsp, stackSize);
        asm.mov(rax, procedure.ToInt64());
    }
    
    public void* SimpleFunc()
    {
        var asm = AllocEmitter();

        asm.mov(eax, 45);
        asm.ret();

        return RecollectExecutableMemory(asm);
    }
    
    public void* WrapNativeCall(IntPtr procedure, List<VeinArgumentRef> Arguments,
        void* returnValue, VeinTypeCode returnType)
    {
        if (Arguments.Any(x => x.IsGeneric))
            throw new NotSupportedException("generics is not support");

        var asm = AllocEmitter();
        var stackOffset = 0;
        var desc = ConvertArgumentToDesc(Arguments);

        var registersUsed = desc.Count(step => step.Instruction != x64_AssemblerStep.InstructionTarget.push);
        var stackSpaceNeeded = desc.Count * 16;

        if (stackSpaceNeeded != 0)
        {
            asm.push(rbp);
            asm.mov(rbp, rsp);
            asm.sub(rsp, stackSpaceNeeded);
        }

        if (returnValue is not null)
        {
            asm.mov(rax, (nint)returnValue);
            asm.mov(__qword_ptr[rsp], rax);
        }
        var sbpOffset = 0;

        for (var i = 0; i < desc.Count; i++)
        {
            var argInfo = desc[i];
            var atype = Arguments[i].ComplexType.Class.TypeCode;

            if (argInfo.Instruction is x64_AssemblerStep.InstructionTarget.push)
            {
                vm.FastFail(WNE.JIT_ASM_GENERATOR_INCORRECT_CAST,
                    $"Direct call is not supported push operations",
                    vm.Frames->Jit);
            }
            else
            {
                if (atype is VeinTypeCode.TYPE_I1 or VeinTypeCode.TYPE_I2)
                    asm.mov(__dword_ptr[rdi - (sbpOffset += argInfo.Register.GetSize())], new AssemblerRegister16(argInfo.Register));
                else if (atype is VeinTypeCode.TYPE_I4)
                {
                    if (argInfo.Register.IsGPR64())
                        asm.mov(__dword_ptr[rdi - (sbpOffset += argInfo.Register.GetSize())],
                            new AssemblerRegister64(argInfo.Register));
                    else if (argInfo.Register.IsGPR32())
                        asm.mov(__dword_ptr[rdi - (sbpOffset += argInfo.Register.GetSize())],
                            new AssemblerRegister32(argInfo.Register));
                    else
                        vm.FastFail(WNE.JIT_ASM_GENERATOR_TYPE_FAULT,
                            $"cannot move int32 into 16/8 bit register.",
                            vm.Frames->Jit);
                }
                else if (atype is VeinTypeCode.TYPE_I8)
                    asm.mov(__dword_ptr[rdi - (sbpOffset += argInfo.Register.GetSize())], new AssemblerRegister64(argInfo.Register));
                else if (atype is VeinTypeCode.TYPE_RAW)
                    asm.mov(__dword_ptr[rdi - (sbpOffset += argInfo.Register.GetSize())], new AssemblerRegister64(argInfo.Register));
                else if (atype is VeinTypeCode.TYPE_R4 or VeinTypeCode.TYPE_R8)
                    asm.movss(__dword_ptr[rdi - (sbpOffset += VeinTypeCode.TYPE_R4.GetNativeSize())], new AssemblerRegisterXMM(argInfo.Register));
                else
                    vm.FastFail(WNE.JIT_ASM_GENERATOR_TYPE_FAULT,
                        $"type code '{atype}' is not supported.",
                        vm.Frames->Jit);
            }
        }


        asm.call((ulong)procedure);

        if (returnValue is not null && returnType is VeinTypeCode.TYPE_R4 or VeinTypeCode.TYPE_R8)
        {
            asm.mov(eax, 4);
            asm.imul(rax, rax, 0);
            asm.mov(rdx, __qword_ptr[rsp]);

            asm.movss(__dword_ptr[rax], xmm0);
        }
        else if (returnValue is not null)
        {
            asm.mov(ecx, 4);
            asm.imul(rcx, rcx, 0);
            asm.mov(rdx, __qword_ptr[rsp]);
            if (returnType is VeinTypeCode.TYPE_I8 or VeinTypeCode.TYPE_U8 or VeinTypeCode.TYPE_RAW)
                asm.mov(__dword_ptr[rdx + rcx], rax);
            else
                asm.mov(__dword_ptr[rdx + rcx], eax);
        }

        if (stackSpaceNeeded != 0)
        {
            asm.add(rsp, stackSpaceNeeded);
            asm.pop(rbp);
        }
        asm.ret();
        if (vm.HasFaulted())
            return null;
        return RecollectExecutableMemory(asm);
    }

    public void* WrapNativeCallStaticVoid(
        IntPtr procedure,
        List<VeinArgumentRef> Arguments,
        stackval* argumentValues,
        void* returnValue, VeinTypeCode returnType)
    {
        var asm = AllocEmitter();
        var stackOffset = 0;
        var desc = ConvertArgumentToDesc(Arguments);

        var registersUsed = desc.Count(step => step.Instruction != x64_AssemblerStep.InstructionTarget.push);

        stackOffset += registersUsed * 8;


        var stackSpaceNeeded = desc.Count * 16;

        if (stackSpaceNeeded != 0)
        {
            asm.push(rbp);
            asm.mov(rbp, rsp);
            asm.sub(rsp, stackSpaceNeeded);
        }

        if (returnValue is not null)
        {
            asm.mov(rax, (nint)returnValue);
            asm.mov(__qword_ptr[rsp], rax);
        }
        

        var index = 0;

        foreach (var argInfo in desc)
        {
            var val = argumentValues[index++];
            if (argInfo.Instruction is x64_AssemblerStep.InstructionTarget.push && val.type == VeinTypeCode.TYPE_RAW)
                asm.push(__[argInfo.StackOffset]);
            else if (argInfo.Instruction is x64_AssemblerStep.InstructionTarget.push)
            {
                if (val.type is VeinTypeCode.TYPE_I1)
                    asm.push(val.data.ub);
                else if (val.type is VeinTypeCode.TYPE_I2)
                    asm.push(val.data.s);
                else if (val.type is VeinTypeCode.TYPE_I4)
                {
                    asm.mov(__dword_ptr[rsp + stackOffset], val.data.i);
                    stackOffset += 8;
                }
                else vm.FastFail(WNE.JIT_ASM_GENERATOR_TYPE_FAULT,
                        $"type code '{val.type}' is not supported.",
                        vm.Frames->Jit);
            }
            else
            {
                
                if (val.type is VeinTypeCode.TYPE_I1)
                    asm.mov(new AssemblerRegister64(argInfo.Register), val.data.ub);
                else if (val.type is VeinTypeCode.TYPE_I2)
                    asm.mov(new AssemblerRegister64(argInfo.Register), val.data.s);
                else if (val.type is VeinTypeCode.TYPE_I4)
                {
                    if (argInfo.Register.IsGPR64())
                        asm.mov(new AssemblerRegister64(argInfo.Register), val.data.i);
                    else if (argInfo.Register.IsGPR32())
                        asm.mov(new AssemblerRegister32(argInfo.Register), val.data.i);
                    else
                        vm.FastFail(WNE.JIT_ASM_GENERATOR_TYPE_FAULT,
                            $"cannot move int32 into 16/8 bit register.",
                            vm.Frames->Jit);
                }
                else if (val.type is VeinTypeCode.TYPE_I8)
                    asm.mov(new AssemblerRegister64(argInfo.Register), val.data.l);
                else if (val.type is VeinTypeCode.TYPE_RAW)
                    asm.mov(new AssemblerRegister64(argInfo.Register), __[argumentValues[index++].data.p]);
                else
                    vm.FastFail(WNE.JIT_ASM_GENERATOR_TYPE_FAULT,
                        $"type code '{val.type}' is not supported.",
                        vm.Frames->Jit);
            }
        }
        

        asm.call((ulong)procedure);
        //asm.nop(1);

        if (returnValue is not null)
        {
            asm.mov(ecx, 4);
            asm.imul(rcx, rcx, 0);
            asm.mov(rdx, __qword_ptr[rsp]);
            if (returnType is VeinTypeCode.TYPE_I8 or VeinTypeCode.TYPE_U8 or VeinTypeCode.TYPE_RAW)
                asm.mov(__dword_ptr[rdx + rcx], rax);
            else
                asm.mov(__dword_ptr[rdx + rcx], eax);
        }

        if (stackSpaceNeeded != 0)
        {
            asm.add(rsp, stackSpaceNeeded);
            asm.pop(rbp);
        }
        asm.ret();
        if (vm.HasFaulted())
            return null;
        return RecollectExecutableMemory(asm);
    }

    public void* WrapNativeCallDetailed(
        IntPtr procedure,
        IntPtr retMemory,
        int argCount,
        IntPtr argsPointer)
    {
        var c = AllocEmitter();

        var copy_args_loop = c.CreateLabel("copy_args_loop");


        var @ref = (NativeCallData*)NativeMemory.AllocZeroed(
            (nuint)sizeof(NativeCallData));

        @ref->argCount = argCount;
        @ref->argsPointer = argsPointer;
        @ref->procedure = procedure;
        @ref->returnMemoryPointer = retMemory;

        // define struct call info
        // c.mov(rdi, (nint)@ref);
        //c.mov(rdi, __[rsp + 0]);
        //c.mov(r11, __[rdi + 8]); // procedure
        //c.mov(rcx, __[rdi + 4 + 8]);
        //c.mov(rdx, __[rdi + 4 + 8 + 8]);
        //c.mov(r8, __[rdi + 8 + 4 + 8 + 8]);
        //c.push(rdi);
        c.push(rsp);
        c.mov(rdi, __[rsp + 0]);
        c.mov(r11, __[rdi + 8]); // procedure
        c.mov(rcx, __[rdi + 16]); // argCount
        c.mov(rdx, __[rdi + 24]); // retMemory
        c.mov(r8, __[rdi + 32]); // argsPointer

        // create stack
        c.push(rbp);
        c.sub(rsp, 0x20);
        c.lea(rbp, __[rsp + 0x20]);

        // copy args
        c.mov(rsi, rcx);
        c.mov(rdi, r8);
        c.mov(rax, 0);
        c.Label(ref copy_args_loop);
        {
            c.mov(r9, __[rdi + rax * 8]);
            c.mov(__[rsp + rax * 8], r9);
            c.inc(rax);
            c.cmp(rax, rsi);
            c.jl(copy_args_loop);
        }

        // call procedure
        c.call(r11);

        // save result
        c.mov(rax, __[rsp]);
        c.mov(__[rdx], rax);

        // restore stack and return
        c.add(rsp, 0x20);
        c.pop(rbp);
        //c.ret();

        var calle = RecollectExecutableMemory(c);


        ((delegate*<NativeCallData*, void>)calle)(@ref);

        return null;
    }

    
    public void* WrapNativeCall_WithArg_Int32(void* procedure, long value)
    {
        var c = AllocEmitter();
        var handle = new IntPtr(procedure).ToInt64();
        c.sub(rsp, 0x40); // allocate stack, minimum size is 0x28
        c.lea(rbp, __[rsp+0x40]);

        c.mov(rax, handle); // move procedure to rax register

        c.mov(rcx, value);

        c.add(rsp, 0x40); // return stack

        c.jmp(rax);

        return RecollectExecutableMemory(c);
    }


    public void* WrapNativeCall(void* procedure, void* returnMemory, void* argsMemory)
    {
        var c = AllocEmitter();
        var handle = new IntPtr(procedure).ToInt64();
        
        //c.push(rbp);
        // register spillage
        //c.sub(rsp, 0x28);
        //c.lea(rbp, __[rsp+0x28]);

        c.test(r11, r11);
        c.mov(r11, handle);
        c.push(rbp);
        c.mov(rbp, rsp);
        c.sub(rsp, 16);

        if (argsMemory is not null)
        {
            c.mov(rax, new IntPtr(argsMemory).ToInt64());
            c.mov(edi, __[rax]);
        }
        
        c.call(r11);
        c.mov(__[rbp - 4], eax);
        c.lea(rax, __[rbp - 4]);
        c.mov(__[new IntPtr(returnMemory).ToInt64()], rax);
        c.add(rsp, 16);
        c.pop(rbp);
        c.ret();

        //c.mov(rdx, __[rax + 8]);
        //c.mov(edx, __[rdx]);
        //c.mov(__[rsp+0x24], edx);
        //c.lea(rdx, __[rsp+0x24]);
        //c.mov(__[rax+0x10], rdx);
        //c.add(rsp, 0x28);
        //c.ret();

        //if (returnMemory != null)
        //{
        //    c.mov(rdx, ((IntPtr)returnMemory).ToInt64());
        //    c.mov(__[rdx], rax);
        //}
        //else
        //    c.nop();

        //c.add(rsp, 0x28); // cleanup
        //c.pop(rbp);
        //c.ret();

        return RecollectExecutableMemory(c);
    }
    
    static ConsoleColor GetColor(FormatterTextKind kind)
    {
        switch (kind)
        {
            case FormatterTextKind.Directive:
            case FormatterTextKind.Keyword:
                return ConsoleColor.Yellow;

            case FormatterTextKind.Prefix:
            case FormatterTextKind.Mnemonic:
                return ConsoleColor.Red;

            case FormatterTextKind.Register:
                return ConsoleColor.Magenta;

            case FormatterTextKind.Number:
                return ConsoleColor.Green;

            default:
                return ConsoleColor.White;
        }
    }

    public static void DumpExecutableProcedure(byte[] body)
    {
        var codeReader = new ByteArrayCodeReader(body);
        var decoder = Iced.Intel.Decoder.Create(64, codeReader);

        decoder.IP = 0xDEAD;

        var formatter = new MasmFormatter();
        var output = new AssemblerFormatterOutputImpl();


        throw null;
        foreach (var instr in decoder)
        {
            output.List.Clear();
            formatter.Format(instr, output);
            foreach (var (text, kind) in output.List)
            {
                Console.ForegroundColor = GetColor(kind);
                Console.Write(text);
            }
            Console.WriteLine();
        }
        Console.ResetColor();
    }

    private void* RecollectExecutableMemory(Assembler asm)
    {
        using var stream = new MemoryStream();
        var r = asm.Assemble(new StreamCodeWriter(stream), 0);
        var asm_code = stream.ToArray();

        var codeReader = new ByteArrayCodeReader(asm_code);
        var decoder = Decoder.Create(64, codeReader);

        decoder.IP = 0xDEAD;

        var formatter = new MasmFormatter();
        var output = new AssemblerFormatterOutputImpl();


        throw null; // coloring

        foreach (var instr in decoder)
        {
            output.List.Clear();
            formatter.Format(instr, output);
            foreach (var (text, kind) in output.List)
            {
                Console.ForegroundColor = GetColor(kind);
                Console.Write(text);
            }
            Console.WriteLine();
        }
        Console.ResetColor();


        var asm_size = (uint)asm_code.Length;
        void* asm_mem = NativeApi.VirtualAlloc(null, asm_size,  NativeApi.AllocationType.Commit,  NativeApi.MemoryProtection.ReadWrite);
        Marshal.Copy(asm_code, 0, new IntPtr(asm_mem), asm_code.Length);
        FlushInstructions(asm_mem, asm_size);
        if (!NativeApi.VirtualProtect(asm_mem, asm_size, NativeApi.Protection.PAGE_EXECUTE_READ, out _))
        {
            vm.FastFail(WNE.STATE_CORRUPT, "virtual protect failed set PAGE_EXECUTE_READ", vm.Frames->Jit);
            return null;
        }
        return asm_mem; 
    }
    public static void FlushInstructions(void* ipBaseAddr, uint size)
        => NativeApi.FlushInstructionCache((void*)Process.GetCurrentProcess().Handle, ipBaseAddr, size);
}
