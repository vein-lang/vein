namespace ishtar;

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Iced.Intel;
using static Iced.Intel.AssemblerRegisters;

public unsafe static class IshtarJIT
{
    private static readonly CallFrame JITFrame = IshtarFrames.Jit();
    public static Architecture Architecture => RuntimeInformation.ProcessArchitecture;
    private static void* _processHandle;
    public static void* ProcessHandle
        => _processHandle == null ?
           _processHandle = Process.GetCurrentProcess().Handle.ToPointer() :
           _processHandle;

    // x86 is not support, but need safe apply arm32
    public static Assembler AllocEmitter()
        => new Assembler(64);

    public static void* WrapNativeCall(void* procedureHandle)
        => WrapNativeCall(new IntPtr(procedureHandle)).ToPointer();
    public static IntPtr WrapNativeCall(IntPtr procedureHandle)
    {
        if (Architecture is Architecture.Arm64 or Architecture.Arm)
            throw new NotSupportedException("Arm32/64 not support");
        if (!Environment.Is64BitProcess)
            throw new NotSupportedException("x86 not support");
        var c = AllocEmitter();
        
        //c.test(ecx, ecx);
        //c.push((uint)procedureHandle.ToInt32());

        c.test(r11, r11);
        c.push(rbp);
        c.sub(rsp, 0x20);
        c.lea(rbp, __[rsp+0x20]);
        c.mov(r11, procedureHandle.ToInt64());
        c.call(r11);
        c.nop();
        c.add(rsp, 0x20);
        c.pop(rbp);
        c.ret();

        return RecollectExecuteMemory(c);
    }

    public static void* WrapNativeCall(void* procedureHandle, void** retMemory)
        => WrapNativeCall(new IntPtr(procedureHandle), new IntPtr(retMemory)).ToPointer();
    public static IntPtr WrapNativeCall(IntPtr procedureHandle, IntPtr retMemory)
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

        return RecollectExecuteMemory(c);
    }


    private static IntPtr RecollectExecuteMemory(Assembler asm)
    {
        using var stream = new MemoryStream();
        var r = asm.Assemble(new StreamCodeWriter(stream), 0);
        var asm_code = stream.ToArray();
        var asm_size = (uint)asm_code.Length;
        void* asm_mem = NativeApi.VirtualAlloc(null, asm_size,  NativeApi.AllocationType.Commit,  NativeApi.MemoryProtection.ReadWrite);
        Marshal.Copy(asm_code, 0, new IntPtr(asm_mem), asm_code.Length);
        FlushInstructions(asm_mem, asm_size);
        var isProtected = NativeApi.VirtualProtect(asm_mem, asm_size, NativeApi.Protection.PAGE_EXECUTE_READ, out _);
        if (!isProtected)
        {
            VM.FastFail(WNE.STATE_CORRUPT, "virtual protect failed set PAGE_EXECUTE_READ", JITFrame);
            return IntPtr.Zero;
        }
        return new IntPtr(asm_mem); //(delegate*<void>)asm_mem;
    }

    public static void FlushInstructions(void* ipBaseAddr, uint size)
        => NativeApi.FlushInstructionCache(ProcessHandle, ipBaseAddr, size);
}


//c.push(rbp);
//c.sub(rsp, 0x20);
//c.lea(rbp, __[rsp+0x20]);
//c.mov(r11, procedureHandle.ToInt64());
//c.jmp(r11);

//c.nop();
//c.mov(__[retMemory.ToInt64()], rax);
//c.pop(rbp);

/*C.ret()
    L000a: call 0x00007ffca8030460
    L000f: mov rdx, 0x432322245
    L0019: mov [rdx], rax
    L001c: add rsp, 0x20
    L0020: pop rbp
    L0021: ret
*/

/*
C.ret(Int32)
    L000a: mov [rbp+0x10], ecx
    L000d: mov ecx, [rbp+0x10]

    L0010: call 0x00007ffca8040460
    L0015: mov rdx, 0x432322245
    L001f: mov [rdx], rax
    L0022: add rsp, 0x20
    L0026: pop rbp
    L0027: ret*/

/*C.ret(Int32, Int32)
    L000a: mov [rbp+0x10], ecx
    L000d: mov [rbp+0x18], edx
    L0010: mov ecx, [rbp+0x10]
    L0013: mov edx, [rbp+0x18]
    L0016: call 0x00007ffca8060460
    L001b: mov rdx, 0x432322245
    L0025: mov [rdx], rax
    L0028: add rsp, 0x20
    L002c: pop rbp
    L002d: ret
*/
