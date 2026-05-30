namespace ishtar.jit;

using ishtar.collections;
using ishtar.runtime.gc;

/// <summary>
/// Method JIT compiler: takes a Vein bytecode method and produces a native x64 function.
/// Integrates: bytecode→IR → optimization → register allocation → code generation.
///
/// Usage:
///   var compiled = MethodCompiler.Compile(method, allocator, OptLevel.O2);
///   // compiled is a void(stackval* args, stackval* result) function pointer
/// </summary>
public static unsafe class MethodCompiler
{
    /// <summary>
    /// JIT-compile a method to native x64 code.
    /// Returns a function pointer with signature: void(stackval* args, stackval* result)
    /// </summary>
    public static void* Compile(RuntimeIshtarMethod* method, AllocatorBlock allocator, OptLevel level = OptLevel.O2)
    {
        // 1. Build IR from bytecode
        var fn = BytecodeToIRBuilder.Build(method, allocator);

        // 2. Run optimization passes
        OptimizationPipeline.Optimize(fn, level);

        // 3. Generate native code
        var code = X64CodeGenerator.Compile(fn);

        // 4. Free IR (code is in executable memory, IR is no longer needed)
        IRFunction.Free(fn);

        return code;
    }

    /// <summary>
    /// Try to JIT-compile a method. If eligible, compiles it, stores the native pointer
    /// in PIInfo.compiled_func_ref, and sets MethodFlags.Jit.
    /// Returns true if the method was successfully compiled.
    /// </summary>
    public static bool TryJitCompile(RuntimeIshtarMethod* method, AllocatorBlock allocator, OptLevel level = OptLevel.O2)
    {
        if (!IsEligible(method))
            return false;

        // 1. Build IR
        var fn = BytecodeToIRBuilder.Build(method, allocator);

        // 2. Optimize
        OptimizationPipeline.Optimize(fn, level);

        // 3. Generate native code (get raw bytes)
        X64CodeGenerator.Compile(fn, out var machineCode);

        // 4. Free IR
        IRFunction.Free(fn);

        // 5. Allocate executable memory and store pointer
        var execMem = ExecutableMemory.Alloc(machineCode);

        method->PIInfo = new PInvokeInfo
        {
            isInternal = false,
            compiled_func_ref = (nint)execMem
        };
        method->JitCodeSize = (uint)machineCode.Length;
        method->SetJitted();

        return true;
    }

    /// <summary>
    /// Check if a method is eligible for JIT compilation.
    /// Currently excludes: native methods, abstract methods, very large methods.
    /// </summary>
    public static bool IsEligible(RuntimeIshtarMethod* method)
    {
        if (method->IsExtern) return false;
        if (method->IsAbstract) return false;
        if (method->Header == null) return false;
        if (method->Header->code == null) return false;
        if (method->Header->code_size == 0) return false;

        // Skip extremely large methods (> 4K opcodes) — interpret those
        if (method->Header->code_size > 4096) return false;

        // Skip methods with exception handlers (for now)
        if (method->Header->exception_handler_list != null &&
            method->Header->exception_handler_list->Count > 0)
            return false;

        return true;
    }
}
