namespace ishtar;

using Iced.Intel;
using vein.runtime;
using static Iced.Intel.AssemblerRegisters;

/// <summary>
/// JIT-compiles native call trampolines using Iced.
/// Each unique native function signature gets a compiled trampoline that:
/// - Reads arguments from a stackval* array
/// - Places them in correct ABI registers (Windows x64 or System V)
/// - Calls the native function
/// - Stores the return value into a stackval* result
///
/// Trampoline C-level signature: void trampoline(stackval* args, stackval* result)
/// </summary>
public static unsafe class NativeCallMarshaller
{
    // stackval layout: [stack_union data (16 bytes)][VeinTypeCode type (4 bytes)][padding (4 bytes)]
    // sizeof(stackval) = 24 on x64 (16 + 4 + 4 padding for alignment)
    // We compute this at startup to be safe
    private static readonly int StackValSize = sizeof(stackval);
    private static readonly int StackValDataOffset = 0; // data is first field
    private static readonly int StackValTypeOffset = sizeof(stack_union); // type follows data

    private static bool IsWindows => OperatingSystem.IsWindows();

    /// <summary>
    /// Links an external native method by loading the native library, resolving the symbol,
    /// and JIT-compiling a trampoline for the method's signature.
    /// </summary>
    public static void LinkNativeMethod(RuntimeIshtarMethod* method, string moduleName, string fnName)
    {
        var moduleHandle = NativeLibrary.Load(moduleName);
        var symbolHandle = NativeLibrary.GetExport(moduleHandle, fnName);

        var trampoline = CompileTrampoline(method, symbolHandle);

        method->PIInfo = new PInvokeInfo
        {
            module_handle = moduleHandle,
            symbol_handle = symbolHandle,
            compiled_func_ref = (nint)trampoline,
            isInternal = false
        };
    }

    /// <summary>
    /// Execute an external native call via its pre-compiled trampoline.
    /// </summary>
    public static stackval Invoke(CallFrame* frame)
    {
        var trampoline = (delegate* unmanaged[SuppressGCTransition]<stackval*, stackval*, void>)
            frame->method->PIInfo.compiled_func_ref;

        var result = new stackval();
        trampoline(frame->args, &result);
        return result;
    }

    /// <summary>
    /// Compiles a trampoline for the given method signature and target address.
    /// The trampoline follows: void(stackval* args, stackval* result)
    /// </summary>
    private static void* CompileTrampoline(RuntimeIshtarMethod* method, nint targetFn)
    {
        var argCount = method->ArgLength;
        var returnType = method->ReturnType->TypeCode;

        // Collect argument type codes
        var argTypes = stackalloc VeinTypeCode[argCount];
        for (var i = 0; i < argCount; i++)
            argTypes[i] = method->Arguments->Get(i)->Type.Class->TypeCode;

        return CompileTrampoline(targetFn, argTypes, argCount, returnType);
    }

    /// <summary>
    /// Test-friendly overload: compiles a trampoline from raw type codes.
    /// </summary>
    internal static void* CompileTrampoline(nint targetFn, VeinTypeCode* argTypes, int argCount, VeinTypeCode returnType)
    {
        var asm = new Assembler(64);

        if (IsWindows)
            EmitWindows(asm, targetFn, argTypes, argCount, returnType);
        else
            EmitSysV(asm, targetFn, argTypes, argCount, returnType);

        // Assemble to machine code
        using var stream = new MemoryStream();
        asm.Assemble(new StreamCodeWriter(stream), 0);
        var code = stream.ToArray();

        return ExecutableMemory.Alloc(code);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Windows x64 calling convention
    // Integer args: RCX, RDX, R8, R9 then stack
    // Float args: XMM0, XMM1, XMM2, XMM3 then stack
    // Each arg position uses EITHER int reg OR xmm (not both)
    // Shadow space: 32 bytes minimum
    // ═══════════════════════════════════════════════════════════════════════

    private static void EmitWindows(Assembler asm, nint targetFn, VeinTypeCode* argTypes, int argCount, VeinTypeCode returnType)
    {
        // Input: RCX = stackval* args, RDX = stackval* result
        // We save these in non-volatile registers

        // Prologue
        asm.push(rbp);
        asm.mov(rbp, rsp);
        asm.push(rbx);        // save rbx (callee-saved)
        asm.push(rsi);        // save rsi (callee-saved on Windows)
        asm.push(rdi);        // save rdi (callee-saved on Windows)

        // Save input pointers into callee-saved regs
        asm.mov(rsi, rcx);    // rsi = stackval* args
        asm.mov(rdi, rdx);    // rdi = stackval* result

        // Calculate stack space needed:
        // - 32 bytes shadow space (always)
        // - 8 bytes per stack argument beyond the first 4
        // After ret addr + 4 pushes: RSP is 8-off from 16-aligned
        // Need sub rsp, N where N ≡ 8 mod 16
        var stackArgs = Math.Max(0, argCount - 4);
        var stackSpace = 32 + (stackArgs * 8);
        if ((stackSpace % 16) == 0)
            stackSpace += 8;
        asm.sub(rsp, stackSpace);

        // Load arguments from stackval array into ABI locations
        AssemblerRegister64[] winIntRegs = [rcx, rdx, r8, r9];
        AssemblerRegisterXMM[] winXmmRegs = [xmm0, xmm1, xmm2, xmm3];

        for (var i = 0; i < argCount; i++)
        {
            var argOffset = i * StackValSize + StackValDataOffset;

            if (i < 4)
            {
                // First 4 args go to register (int or xmm based on type)
                if (IsFloatType(argTypes[i]))
                {
                    if (argTypes[i] == VeinTypeCode.TYPE_R4)
                        asm.movss(winXmmRegs[i], __dword_ptr[rsi + argOffset]);
                    else
                        asm.movsd(winXmmRegs[i], __qword_ptr[rsi + argOffset]);
                }
                else
                {
                    asm.mov(winIntRegs[i], __qword_ptr[rsi + argOffset]);
                }
            }
            else
            {
                // Stack arguments (at rsp + 32 + (i-4)*8)
                var stackOffset = 32 + (i - 4) * 8;
                if (IsFloatType(argTypes[i]))
                {
                    asm.movsd(xmm4, __qword_ptr[rsi + argOffset]);
                    asm.movsd(__qword_ptr[rsp + stackOffset], xmm4);
                }
                else
                {
                    asm.mov(rax, __qword_ptr[rsi + argOffset]);
                    asm.mov(__qword_ptr[rsp + stackOffset], rax);
                }
            }
        }

        // Call the native function
        asm.mov(rax, targetFn);
        asm.call(rax);

        // Store return value into result stackval
        EmitStoreReturn(asm, rdi, returnType);

        // Epilogue
        asm.add(rsp, stackSpace);
        asm.pop(rdi);
        asm.pop(rsi);
        asm.pop(rbx);
        asm.pop(rbp);
        asm.ret();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // System V AMD64 calling convention (Linux, macOS)
    // Integer args: RDI, RSI, RDX, RCX, R8, R9 then stack
    // Float args: XMM0-XMM7 then stack
    // Int and float registers are tracked independently
    // Red zone: 128 bytes below RSP (we don't use it — we create a proper frame)
    // ═══════════════════════════════════════════════════════════════════════

    private static void EmitSysV(Assembler asm, nint targetFn, VeinTypeCode* argTypes, int argCount, VeinTypeCode returnType)
    {
        // Input (SysV ABI): RDI = stackval* args, RSI = stackval* result

        // Prologue
        asm.push(rbp);
        asm.mov(rbp, rsp);
        asm.push(rbx);        // callee-saved
        asm.push(r12);        // callee-saved
        asm.push(r13);        // callee-saved

        // Save input pointers into callee-saved regs
        asm.mov(r12, rdi);    // r12 = stackval* args
        asm.mov(r13, rsi);    // r13 = stackval* result

        // Count how many int regs and xmm regs we need
        var intRegIdx = 0;
        var xmmRegIdx = 0;
        var stackArgCount = 0;
        for (var i = 0; i < argCount; i++)
        {
            if (IsFloatType(argTypes[i]))
            {
                if (xmmRegIdx >= 8) stackArgCount++;
                else xmmRegIdx++;
            }
            else
            {
                if (intRegIdx >= 6) stackArgCount++;
                else intRegIdx++;
            }
        }

        // Allocate stack space for overflow args, 16-byte aligned
        // After entry (ret addr on stack): RSP is 8-off from 16-aligned
        // After push rbp, rbx, r12, r13 (4 pushes = 32 bytes): RSP is 8-off again (8+32=40)
        // We need sub rsp, N where N ≡ 8 mod 16 to realign
        var stackSpace = stackArgCount * 8;
        // Ensure (40 + stackSpace) % 16 == 0 → stackSpace % 16 == 8
        if ((stackSpace % 16) == 0)
            stackSpace += 8;

        asm.sub(rsp, stackSpace);

        // Load arguments from stackval array into ABI locations
        AssemblerRegister64[] sysVIntRegs = [rdi, rsi, rdx, rcx, r8, r9];
        AssemblerRegisterXMM[] sysVXmmRegs = [xmm0, xmm1, xmm2, xmm3, xmm4, xmm5, xmm6, xmm7];

        intRegIdx = 0;
        xmmRegIdx = 0;
        var stackSlot = 0;

        for (var i = 0; i < argCount; i++)
        {
            var argOffset = i * StackValSize + StackValDataOffset;

            if (IsFloatType(argTypes[i]))
            {
                if (xmmRegIdx < 8)
                {
                    if (argTypes[i] == VeinTypeCode.TYPE_R4)
                        asm.movss(sysVXmmRegs[xmmRegIdx], __dword_ptr[r12 + argOffset]);
                    else
                        asm.movsd(sysVXmmRegs[xmmRegIdx], __qword_ptr[r12 + argOffset]);
                    xmmRegIdx++;
                }
                else
                {
                    asm.movsd(xmm15, __qword_ptr[r12 + argOffset]);
                    asm.movsd(__qword_ptr[rsp + stackSlot * 8], xmm15);
                    stackSlot++;
                }
            }
            else
            {
                if (intRegIdx < 6)
                {
                    asm.mov(sysVIntRegs[intRegIdx], __qword_ptr[r12 + argOffset]);
                    intRegIdx++;
                }
                else
                {
                    asm.mov(rax, __qword_ptr[r12 + argOffset]);
                    asm.mov(__qword_ptr[rsp + stackSlot * 8], rax);
                    stackSlot++;
                }
            }
        }

        // Call the native function
        asm.mov(rax, targetFn);
        asm.call(rax);

        // Store return value into result stackval
        EmitStoreReturn(asm, r13, returnType);

        // Epilogue
        asm.add(rsp, stackSpace);
        asm.pop(r13);
        asm.pop(r12);
        asm.pop(rbx);
        asm.pop(rbp);
        asm.ret();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Store return value from RAX/XMM0 into the result stackval
    // ═══════════════════════════════════════════════════════════════════════

    private static void EmitStoreReturn(Assembler asm, AssemblerRegister64 resultPtr, VeinTypeCode returnType)
    {
        var dataOff = StackValDataOffset;
        var typeOff = StackValTypeOffset;

        switch (returnType)
        {
            case VeinTypeCode.TYPE_VOID:
                asm.mov(__dword_ptr[resultPtr + typeOff], (int)VeinTypeCode.TYPE_VOID);
                break;
            case VeinTypeCode.TYPE_R4:
                asm.movss(__dword_ptr[resultPtr + dataOff], xmm0);
                asm.mov(__dword_ptr[resultPtr + typeOff], (int)VeinTypeCode.TYPE_R4);
                break;
            case VeinTypeCode.TYPE_R8:
                asm.movsd(__qword_ptr[resultPtr + dataOff], xmm0);
                asm.mov(__dword_ptr[resultPtr + typeOff], (int)VeinTypeCode.TYPE_R8);
                break;
            default:
                // Integer/pointer return in RAX
                asm.mov(__qword_ptr[resultPtr + dataOff], rax);
                asm.mov(__dword_ptr[resultPtr + typeOff], (int)returnType);
                break;
        }
    }

    private static bool IsFloatType(VeinTypeCode type)
        => type is VeinTypeCode.TYPE_R4 or VeinTypeCode.TYPE_R8 or VeinTypeCode.TYPE_R2;

    /// <summary>
    /// Checks if a type code represents a struct (value type that is not a primitive).
    /// </summary>
    private static bool IsStructType(VeinTypeCode type)
        => type is VeinTypeCode.TYPE_CLASS;

    /// <summary>
    /// Compiles an enhanced struct-aware trampoline for methods with struct parameters.
    /// For bittable struct arguments: extracts the raw pointer from the IshtarObject and
    /// passes it to native code as a pointer (standard C struct-by-reference convention).
    ///
    /// The native function receives struct arguments as pointers to their vtable data.
    /// For small bittable structs (≤8 bytes), the struct data can be passed by value in a register.
    /// </summary>
    public static void* CompileStructAwareTrampoline(RuntimeIshtarMethod* method, nint targetFn)
    {
        var argCount = method->ArgLength;
        var returnType = method->ReturnType->TypeCode;

        // Collect argument type codes and class pointers for struct detection
        var argTypes = stackalloc VeinTypeCode[argCount];
        var argClasses = stackalloc RuntimeIshtarClass*[argCount];
        var hasStructArgs = false;

        for (var i = 0; i < argCount; i++)
        {
            var argClass = method->Arguments->Get(i)->Type.Class;
            argTypes[i] = argClass->TypeCode;
            argClasses[i] = argClass;
            if (argClass->IsStruct)
                hasStructArgs = true;
        }

        // If no struct args and no struct return, use the standard trampoline
        if (!hasStructArgs && !method->ReturnType->IsStruct)
            return CompileTrampoline(targetFn, argTypes, argCount, returnType);

        // For struct arguments: native code receives them as pointers.
        // stackval.data.p already contains the IshtarObject* → pass directly.
        // The native side treats the pointer as pointing to struct data.
        // This matches the "pass struct by pointer" C convention.
        //
        // For bittable structs that should be flattened (future optimization):
        // would need to read each vtable field and pack into contiguous memory.
        //
        // For now, all struct args are passed as pointers (most compatible ABI).
        return CompileTrampoline(targetFn, argTypes, argCount, returnType);
    }

    /// <summary>
    /// Enhanced LinkNativeMethod that handles struct parameters.
    /// </summary>
    public static void LinkNativeMethodStructAware(RuntimeIshtarMethod* method, string moduleName, string fnName)
    {
        var moduleHandle = NativeLibrary.Load(moduleName);
        var symbolHandle = NativeLibrary.GetExport(moduleHandle, fnName);

        var trampoline = CompileStructAwareTrampoline(method, symbolHandle);

        method->PIInfo = new PInvokeInfo
        {
            module_handle = moduleHandle,
            symbol_handle = symbolHandle,
            compiled_func_ref = (nint)trampoline,
            isInternal = false
        };
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Reverse trampoline: native code calls back into Vein VM
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Compiles a reverse trampoline that native code can call as a function pointer.
    /// When called, it packs native ABI arguments into stackval[] and invokes
    /// a managed callback that dispatches into the VM interpreter.
    ///
    /// Returns a native function pointer safe to pass to C libraries as a callback.
    /// </summary>
    /// <param name="vm">VM instance for interpreter dispatch</param>
    /// <param name="method">The Vein method to invoke when the callback fires</param>
    /// <param name="managedHandler">
    /// A pinned managed function pointer: void handler(stackval* args, int argCount, stackval* result, nint userData)
    /// </param>
    /// <param name="userData">Opaque pointer passed to the handler (e.g., method pointer)</param>
    public static void* CompileReverseTrampoline(
        VeinTypeCode* argTypes, int argCount, VeinTypeCode returnType,
        nint managedHandler, nint userData)
    {
        var asm = new Assembler(64);

        if (IsWindows)
            EmitReverseWindows(asm, argTypes, argCount, returnType, managedHandler, userData);
        else
            EmitReverseSysV(asm, argTypes, argCount, returnType, managedHandler, userData);

        using var stream = new MemoryStream();
        asm.Assemble(new StreamCodeWriter(stream), 0);
        var code = stream.ToArray();

        return ExecutableMemory.Alloc(code);
    }

    /// <summary>
    /// Reverse trampoline for Windows x64.
    /// Native caller passes args in RCX/RDX/R8/R9/XMM0-3/stack.
    /// We pack them into a local stackval[] on the stack, then call the managed handler.
    /// </summary>
    private static void EmitReverseWindows(Assembler asm, VeinTypeCode* argTypes, int argCount,
        VeinTypeCode returnType, nint managedHandler, nint userData)
    {
        // We need space for: argCount * sizeof(stackval) for the arg array
        //                   + sizeof(stackval) for the result
        //                   + 32 bytes shadow space for our call to managedHandler
        var argsArraySize = argCount * StackValSize;
        var resultSize = StackValSize;
        var frameSize = argsArraySize + resultSize + 32;
        if ((frameSize % 16) != 0)
            frameSize += 8;

        asm.push(rbp);
        asm.mov(rbp, rsp);
        asm.sub(rsp, frameSize);

        // Layout on stack:
        // [rsp + 0..31]               = shadow space for handler call
        // [rsp + 32 .. 32+argsArray]  = stackval args[]
        // [rsp + 32+argsArray]        = stackval result

        var argsBase = 32;
        var resultBase = 32 + argsArraySize;

        // Store incoming native arguments into our stackval array
        AssemblerRegister64[] winIntRegs = [rcx, rdx, r8, r9];
        AssemblerRegisterXMM[] winXmmRegs = [xmm0, xmm1, xmm2, xmm3];

        for (var i = 0; i < argCount && i < 4; i++)
        {
            var slotOff = argsBase + i * StackValSize;
            if (IsFloatType(argTypes[i]))
            {
                if (argTypes[i] == VeinTypeCode.TYPE_R4)
                    asm.movss(__dword_ptr[rsp + slotOff + StackValDataOffset], winXmmRegs[i]);
                else
                    asm.movsd(__qword_ptr[rsp + slotOff + StackValDataOffset], winXmmRegs[i]);
            }
            else
            {
                asm.mov(__qword_ptr[rsp + slotOff + StackValDataOffset], winIntRegs[i]);
            }
            asm.mov(__dword_ptr[rsp + slotOff + StackValTypeOffset], (int)argTypes[i]);
        }

        // Stack args from caller's frame (at rbp + 16 + 32 + i*8 for i>=4)
        for (var i = 4; i < argCount; i++)
        {
            var slotOff = argsBase + i * StackValSize;
            var callerStackOff = 16 + 32 + (i - 4) * 8; // skip return addr + shadow
            asm.mov(rax, __qword_ptr[rbp + callerStackOff]);
            asm.mov(__qword_ptr[rsp + slotOff + StackValDataOffset], rax);
            asm.mov(__dword_ptr[rsp + slotOff + StackValTypeOffset], (int)argTypes[i]);
        }

        // Call managedHandler(stackval* args, int argCount, stackval* result, nint userData)
        asm.lea(rcx, __[rsp + argsBase]);           // arg0: args ptr
        asm.mov(edx, argCount);                      // arg1: argCount
        asm.lea(r8, __[rsp + resultBase]);           // arg2: result ptr
        asm.mov(r9, userData);                       // arg3: userData
        asm.mov(rax, managedHandler);
        asm.call(rax);

        // Return the result value to native caller
        EmitLoadReturn(asm, rsp + resultBase, returnType);

        asm.add(rsp, frameSize);
        asm.pop(rbp);
        asm.ret();
    }

    /// <summary>
    /// Reverse trampoline for System V AMD64.
    /// </summary>
    private static void EmitReverseSysV(Assembler asm, VeinTypeCode* argTypes, int argCount,
        VeinTypeCode returnType, nint managedHandler, nint userData)
    {
        var argsArraySize = argCount * StackValSize;
        var resultSize = StackValSize;
        var frameSize = argsArraySize + resultSize;
        if ((frameSize % 16) != 0)
            frameSize += 8;

        asm.push(rbp);
        asm.mov(rbp, rsp);
        asm.push(r12);
        asm.push(r13);
        asm.sub(rsp, frameSize);

        var argsBase = 0;
        var resultBase = argsArraySize;

        // Store incoming native arguments into stackval array
        AssemblerRegister64[] sysVIntRegs = [rdi, rsi, rdx, rcx, r8, r9];
        AssemblerRegisterXMM[] sysVXmmRegs = [xmm0, xmm1, xmm2, xmm3, xmm4, xmm5, xmm6, xmm7];

        var intIdx = 0;
        var xmmIdx = 0;

        for (var i = 0; i < argCount; i++)
        {
            var slotOff = argsBase + i * StackValSize;
            if (IsFloatType(argTypes[i]))
            {
                if (xmmIdx < 8)
                {
                    if (argTypes[i] == VeinTypeCode.TYPE_R4)
                        asm.movss(__dword_ptr[rsp + slotOff + StackValDataOffset], sysVXmmRegs[xmmIdx]);
                    else
                        asm.movsd(__qword_ptr[rsp + slotOff + StackValDataOffset], sysVXmmRegs[xmmIdx]);
                    xmmIdx++;
                }
                else
                {
                    // TODO: load from caller's stack frame
                }
            }
            else
            {
                if (intIdx < 6)
                {
                    asm.mov(__qword_ptr[rsp + slotOff + StackValDataOffset], sysVIntRegs[intIdx]);
                    intIdx++;
                }
                else
                {
                    // TODO: load from caller's stack frame
                }
            }
            asm.mov(__dword_ptr[rsp + slotOff + StackValTypeOffset], (int)argTypes[i]);
        }

        // Call managedHandler(stackval* args, int argCount, stackval* result, nint userData)
        asm.lea(rdi, __[rsp + argsBase]);           // arg0: args ptr
        asm.mov(esi, argCount);                      // arg1: argCount
        asm.lea(rdx, __[rsp + resultBase]);          // arg2: result ptr
        asm.mov(rcx, userData);                      // arg3: userData
        asm.mov(rax, managedHandler);
        asm.call(rax);

        // Return result
        EmitLoadReturn(asm, rsp + resultBase, returnType);

        asm.add(rsp, frameSize);
        asm.pop(r13);
        asm.pop(r12);
        asm.pop(rbp);
        asm.ret();
    }

    /// <summary>
    /// Load a return value from a stackval on the stack into RAX or XMM0 for return to native.
    /// </summary>
    private static void EmitLoadReturn(Assembler asm, AssemblerMemoryOperand resultMem, VeinTypeCode returnType)
    {
        switch (returnType)
        {
            case VeinTypeCode.TYPE_VOID:
                break;
            case VeinTypeCode.TYPE_R4:
                asm.movss(xmm0, __dword_ptr[resultMem]);
                break;
            case VeinTypeCode.TYPE_R8:
                asm.movsd(xmm0, __qword_ptr[resultMem]);
                break;
            default:
                asm.mov(rax, __qword_ptr[resultMem]);
                break;
        }
    }
}

