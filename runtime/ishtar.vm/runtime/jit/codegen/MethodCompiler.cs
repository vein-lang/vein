namespace ishtar.jit;

using ishtar;
using ishtar.collections;
using ishtar.runtime;
using ishtar.runtime.gc;
using vein.runtime;

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
        var (code, reason) = GetIneligibilityReason(method);
        if (code != JitRejectCode.None)
        {
            method->JitRejected = true;
            method->JitRejectReasonCode = code;
            return false;
        }

        // 0. Eagerly JIT all call targets that aren't already compiled
        if (!EnsureCallTargetsCompiled(method, allocator, level))
        {
            method->JitRejected = true;
            method->JitRejectReasonCode = JitRejectCode.CallTargetCompilationFailed;
            return false;
        }

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
    /// Currently excludes: native methods, abstract methods, very large methods,
    /// and methods using opcodes not yet supported by BytecodeToIRBuilder.
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

        // Scan bytecode — reject if any unsupported opcode is found
        if (!AllOpcodesSupported(method->Header->code, method->Header->code_size))
            return false;

        // Verify all CALL targets can be resolved and JIT-compiled
        if (!AllCallsResolvable(method))
            return false;

        return true;
    }

    /// <summary>
    /// Returns a reason code + description why the method is not eligible for JIT.
    /// Returns (None, null) if eligible.
    /// </summary>
    public static (JitRejectCode code, string reason) GetIneligibilityReason(RuntimeIshtarMethod* method)
    {
        if (method->IsExtern) return (JitRejectCode.Extern, "extern method");
        if (method->IsAbstract) return (JitRejectCode.Abstract, "abstract method");
        if (method->Header == null) return (JitRejectCode.NullHeader, "null header");
        if (method->Header->code == null) return (JitRejectCode.NullBytecode, "null bytecode");
        if (method->Header->code_size == 0) return (JitRejectCode.EmptyBytecode, "empty bytecode");
        if (method->Header->code_size > 4096) return (JitRejectCode.BytecodeTooLarge, $"bytecode too large ({method->Header->code_size} > 4096)");

        if (method->Header->exception_handler_list != null &&
            method->Header->exception_handler_list->Count > 0)
            return (JitRejectCode.HasExceptionHandlers, "has exception handlers");

        var unsupported = FindFirstUnsupportedOpcode(method->Header->code, method->Header->code_size);
        if (unsupported != null)
            return (JitRejectCode.UnsupportedOpcode, $"unsupported opcode: {unsupported}");

        if (!AllCallsResolvable(method))
            return (JitRejectCode.UnresolvableCalls, "unresolvable or recursive CALL targets");

        return (JitRejectCode.None, null);
    }

    /// <summary>
    /// Checks that all CALL targets in the method can be resolved and are themselves eligible for JIT.
    /// Uses a fixed stack buffer to detect cycles (mutual recursion).
    /// Self-calls in tail position (CALL self immediately followed by RET) are allowed — they become loops.
    /// </summary>
    private const int MaxCallDepth = 32;

    /// <summary>
    /// Returns true if ALL self-recursive calls in the method are in tail position
    /// (CALL self is immediately followed by RET with no intervening opcodes).
    /// </summary>
    public static bool HasOnlyTailSelfCalls(RuntimeIshtarMethod* method)
    {
        var module = method->Owner->Owner;
        var ip = method->Header->code;
        var end = ip + method->Header->code_size;
        var hasSelfCalls = false;

        while (ip < end)
        {
            var opcode = (OpCodeValue)(ushort)*ip;
            ip++;

            switch (opcode)
            {
                case OpCodeValue.CALL:
                {
                    var tokenIdx = *ip; ip++;
                    var ownerIdx = *ip; ip++;

                    if (!module->types_table->TryGetValue((int)ownerIdx, out var ownerName))
                        break;
                    var ownerClass = module->FindType(ownerName, true, false);
                    if (ownerClass == null || ownerClass->IsUnresolved)
                        break;
                    var methodName = module->GetConstStringByIndex((int)tokenIdx);
                    var target = ownerClass->FindMethod(methodName, m => m->Name.Equals(methodName));

                    if (target == method)
                    {
                        hasSelfCalls = true;
                        // Check that next opcode is RET
                        if (ip >= end || (OpCodeValue)(ushort)*ip != OpCodeValue.RET)
                            return false; // non-tail self-call
                    }
                    break;
                }
                case OpCodeValue.LDARG_S:
                case OpCodeValue.LDLOC_S:
                case OpCodeValue.STLOC_S:
                case OpCodeValue.LDC_I4_S:
                case OpCodeValue.LDC_I2_S:
                case OpCodeValue.LDC_F4:
                case OpCodeValue.JMP:
                case OpCodeValue.JMP_T:
                case OpCodeValue.JMP_F:
                    ip++;
                    break;
                case OpCodeValue.LDC_I8_S:
                    ip += 2;
                    break;
                case OpCodeValue.LOC_INIT:
                {
                    var locCount = *ip; ip++;
                    ip += locCount;
                    break;
                }
                default:
                    break;
            }
        }

        return hasSelfCalls;
    }

    private static bool AllCallsResolvable(RuntimeIshtarMethod* method)
    {
        var visited = stackalloc nint[MaxCallDepth];
        visited[0] = (nint)method;
        return AllCallsResolvable(method, visited, 1);
    }

    private static bool AllCallsResolvable(RuntimeIshtarMethod* method, nint* visited, int visitedCount)
    {
        var module = method->Owner->Owner;
        var ip = method->Header->code;
        var end = ip + method->Header->code_size;

        while (ip < end)
        {
            var opcode = (OpCodeValue)(ushort)*ip;
            ip++;

            switch (opcode)
            {
                case OpCodeValue.CALL:
                {
                    var tokenIdx = *ip; ip++;
                    var ownerIdx = *ip; ip++;

                    if (!module->types_table->TryGetValue((int)ownerIdx, out var ownerName))
                        return false;

                    var ownerClass = module->FindType(ownerName, true, false);
                    if (ownerClass == null || ownerClass->IsUnresolved)
                        return false;

                    var methodName = module->GetConstStringByIndex((int)tokenIdx);
                    var target = ownerClass->FindMethod(methodName, m => m->Name.Equals(methodName));
                    if (target == null)
                        return false;

                    // Check for cycles (self-call or mutual recursion)
                    var targetPtr = (nint)target;
                    var isCycle = false;
                    for (var i = 0; i < visitedCount; i++)
                        if (visited[i] == targetPtr) { isCycle = true; break; }

                    if (isCycle)
                    {
                        // Self-calls are OK — they become indirect calls (or TCO loops if tail)
                        if (target == method)
                            break;
                        return false; // mutual recursion not supported
                    }

                    if (target->IsJitted)
                        break;

                    // Target must pass basic eligibility
                    if (!IsEligibleBasic(target))
                        return false;

                    // Recursively check target's calls (with depth limit)
                    if (visitedCount >= MaxCallDepth)
                        return false;

                    visited[visitedCount] = targetPtr;
                    if (!AllCallsResolvable(target, visited, visitedCount + 1))
                        return false;

                    break;
                }
                case OpCodeValue.LDARG_S:
                case OpCodeValue.LDLOC_S:
                case OpCodeValue.STLOC_S:
                case OpCodeValue.LDC_I4_S:
                case OpCodeValue.LDC_I2_S:
                case OpCodeValue.LDC_F4:
                case OpCodeValue.JMP:
                case OpCodeValue.JMP_T:
                case OpCodeValue.JMP_F:
                    ip++;
                    break;
                case OpCodeValue.LDC_I8_S:
                    ip += 2;
                    break;
                case OpCodeValue.LOC_INIT:
                {
                    var locCount = *ip; ip++;
                    ip += locCount;
                    break;
                }
                default:
                    break;
            }
        }

        return true;
    }

    private static bool IsEligibleBasic(RuntimeIshtarMethod* method)
    {
        if (method->IsExtern) return false;
        if (method->IsAbstract) return false;
        if (method->Header == null) return false;
        if (method->Header->code == null) return false;
        if (method->Header->code_size == 0) return false;
        if (method->Header->code_size > 4096) return false;
        if (method->Header->exception_handler_list != null &&
            method->Header->exception_handler_list->Count > 0)
            return false;
        if (!AllOpcodesSupported(method->Header->code, method->Header->code_size))
            return false;
        return true;
    }

    /// <summary>
    /// Eagerly JIT-compile all CALL targets in the method that aren't already compiled.
    /// </summary>
    private static bool EnsureCallTargetsCompiled(RuntimeIshtarMethod* method, AllocatorBlock allocator, OptLevel level)
    {
        var compiling = stackalloc nint[MaxCallDepth];
        compiling[0] = (nint)method;
        return EnsureCallTargetsCompiled(method, allocator, level, compiling, 1);
    }

    private static bool EnsureCallTargetsCompiled(RuntimeIshtarMethod* method, AllocatorBlock allocator, OptLevel level, nint* compiling, int compilingCount)
    {
        var module = method->Owner->Owner;
        var ip = method->Header->code;
        var end = ip + method->Header->code_size;

        while (ip < end)
        {
            var opcode = (OpCodeValue)(ushort)*ip;
            ip++;

            switch (opcode)
            {
                case OpCodeValue.CALL:
                {
                    var tokenIdx = *ip; ip++;
                    var ownerIdx = *ip; ip++;

                    if (!module->types_table->TryGetValue((int)ownerIdx, out var ownerName))
                        return false;

                    var ownerClass = module->FindType(ownerName, true, false);
                    if (ownerClass == null || ownerClass->IsUnresolved)
                        return false;

                    var methodName = module->GetConstStringByIndex((int)tokenIdx);
                    var target = ownerClass->FindMethod(methodName, m => m->Name.Equals(methodName));
                    if (target == null)
                        return false;

                    if (target->IsJitted)
                        break;

                    // Self tail calls become loops — no need to compile target
                    if (target == method)
                        break;

                    // Skip if already in compilation chain
                    var targetPtr = (nint)target;
                    var inChain = false;
                    for (var i = 0; i < compilingCount; i++)
                        if (compiling[i] == targetPtr) { inChain = true; break; }
                    if (inChain)
                        break;

                    if (!IsEligibleBasic(target))
                        return false;

                    if (compilingCount >= MaxCallDepth)
                        return false;

                    // Recursively ensure target's deps are compiled first
                    compiling[compilingCount] = targetPtr;
                    if (!EnsureCallTargetsCompiled(target, allocator, level, compiling, compilingCount + 1))
                        return false;

                    // Now compile the target
                    var childAllocator = IshtarGC.CreateAllocatorWithParent(target);
                    if (!TryJitCompile(target, childAllocator, level))
                        return false;

                    break;
                }
                case OpCodeValue.LDARG_S:
                case OpCodeValue.LDLOC_S:
                case OpCodeValue.STLOC_S:
                case OpCodeValue.LDC_I4_S:
                case OpCodeValue.LDC_I2_S:
                case OpCodeValue.LDC_F4:
                case OpCodeValue.JMP:
                case OpCodeValue.JMP_T:
                case OpCodeValue.JMP_F:
                    ip++;
                    break;
                case OpCodeValue.LDC_I8_S:
                    ip += 2;
                    break;
                case OpCodeValue.LOC_INIT:
                {
                    var locCount = *ip; ip++;
                    ip += locCount;
                    break;
                }
                default:
                    break;
            }
        }

        return true;
    }

    /// <summary>
    /// Returns true only if every opcode in the bytecode stream is handled by BytecodeToIRBuilder.
    /// </summary>
    private static bool AllOpcodesSupported(uint* code, uint codeSize)
    {
        var ip = code;
        var end = code + codeSize;

        while (ip < end)
        {
            var opcode = (OpCodeValue)(ushort)*ip;
            ip++;

            switch (opcode)
            {
                // Arithmetic
                case OpCodeValue.NOP:
                case OpCodeValue.ADD:
                case OpCodeValue.SUB:
                case OpCodeValue.MUL:
                case OpCodeValue.DIV:
                case OpCodeValue.MOD:
                // Bitwise
                case OpCodeValue.XOR:
                case OpCodeValue.OR:
                case OpCodeValue.AND:
                case OpCodeValue.SHR:
                case OpCodeValue.SHL:
                // Load args (no operand)
                case OpCodeValue.LDARG_0:
                case OpCodeValue.LDARG_1:
                case OpCodeValue.LDARG_2:
                case OpCodeValue.LDARG_3:
                case OpCodeValue.LDARG_4:
                case OpCodeValue.LDARG_5:
                // Load/store locals (no extra operand for 0..5)
                case OpCodeValue.LDLOC_0:
                case OpCodeValue.LDLOC_1:
                case OpCodeValue.LDLOC_2:
                case OpCodeValue.LDLOC_3:
                case OpCodeValue.LDLOC_4:
                case OpCodeValue.LDLOC_5:
                case OpCodeValue.STLOC_0:
                case OpCodeValue.STLOC_1:
                case OpCodeValue.STLOC_2:
                case OpCodeValue.STLOC_3:
                case OpCodeValue.STLOC_4:
                case OpCodeValue.STLOC_5:
                // Constants (no operand)
                case OpCodeValue.LDC_I4_0:
                case OpCodeValue.LDC_I4_1:
                case OpCodeValue.LDC_I4_2:
                case OpCodeValue.LDC_I4_3:
                case OpCodeValue.LDC_I4_4:
                case OpCodeValue.LDC_I4_5:
                case OpCodeValue.LDC_I8_0:
                case OpCodeValue.LDC_I8_1:
                case OpCodeValue.LDC_I8_2:
                case OpCodeValue.LDC_I8_3:
                case OpCodeValue.LDC_I8_4:
                case OpCodeValue.LDC_I8_5:
                case OpCodeValue.LDC_I2_0:
                case OpCodeValue.LDC_I2_1:
                case OpCodeValue.LDC_I2_2:
                case OpCodeValue.LDC_I2_3:
                case OpCodeValue.LDC_I2_4:
                case OpCodeValue.LDC_I2_5:
                // Comparisons
                case OpCodeValue.EQL_T:
                case OpCodeValue.EQL_F:
                case OpCodeValue.EQL_NQ:
                case OpCodeValue.EQL_L:
                case OpCodeValue.EQL_LQ:
                case OpCodeValue.EQL_H:
                case OpCodeValue.EQL_HQ:
                // Return
                case OpCodeValue.RET:
                // Stack ops
                case OpCodeValue.DUP:
                case OpCodeValue.POP:
                    break;

                // One operand opcodes
                case OpCodeValue.LDARG_S:
                case OpCodeValue.LDLOC_S:
                case OpCodeValue.STLOC_S:
                case OpCodeValue.LDC_I4_S:
                case OpCodeValue.LDC_I2_S:
                case OpCodeValue.LDC_F4:
                case OpCodeValue.JMP:
                case OpCodeValue.JMP_T:
                case OpCodeValue.JMP_F:
                    ip++; // skip 1 operand
                    break;

                // Two operand (i64 const)
                case OpCodeValue.LDC_I8_S:
                    ip += 2; // lo + hi
                    break;

                // Call (two operands: tokenIdx + ownerIdx)
                case OpCodeValue.CALL:
                    ip += 2;
                    break;

                // LOC_INIT: count + count * typeIdx
                case OpCodeValue.LOC_INIT:
                {
                    var locCount = *ip; ip++;
                    ip += locCount; // skip type indices
                    break;
                }

                // Array len (no operand but needs array support — borderline)
                case OpCodeValue.LDLEN:
                    break;

                default:
                    // Unsupported opcode found
                    return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Returns the name of the first unsupported opcode in the bytecode, or null if all are supported.
    /// </summary>
    private static string FindFirstUnsupportedOpcode(uint* code, uint codeSize)
    {
        var ip = code;
        var end = code + codeSize;

        while (ip < end)
        {
            var opcode = (OpCodeValue)(ushort)*ip;
            ip++;

            switch (opcode)
            {
                case OpCodeValue.NOP:
                case OpCodeValue.ADD:
                case OpCodeValue.SUB:
                case OpCodeValue.MUL:
                case OpCodeValue.DIV:
                case OpCodeValue.MOD:
                case OpCodeValue.XOR:
                case OpCodeValue.OR:
                case OpCodeValue.AND:
                case OpCodeValue.SHR:
                case OpCodeValue.SHL:
                case OpCodeValue.LDARG_0:
                case OpCodeValue.LDARG_1:
                case OpCodeValue.LDARG_2:
                case OpCodeValue.LDARG_3:
                case OpCodeValue.LDARG_4:
                case OpCodeValue.LDARG_5:
                case OpCodeValue.LDLOC_0:
                case OpCodeValue.LDLOC_1:
                case OpCodeValue.LDLOC_2:
                case OpCodeValue.LDLOC_3:
                case OpCodeValue.LDLOC_4:
                case OpCodeValue.LDLOC_5:
                case OpCodeValue.STLOC_0:
                case OpCodeValue.STLOC_1:
                case OpCodeValue.STLOC_2:
                case OpCodeValue.STLOC_3:
                case OpCodeValue.STLOC_4:
                case OpCodeValue.STLOC_5:
                case OpCodeValue.LDC_I4_0:
                case OpCodeValue.LDC_I4_1:
                case OpCodeValue.LDC_I4_2:
                case OpCodeValue.LDC_I4_3:
                case OpCodeValue.LDC_I4_4:
                case OpCodeValue.LDC_I4_5:
                case OpCodeValue.LDC_I8_0:
                case OpCodeValue.LDC_I8_1:
                case OpCodeValue.LDC_I8_2:
                case OpCodeValue.LDC_I8_3:
                case OpCodeValue.LDC_I8_4:
                case OpCodeValue.LDC_I8_5:
                case OpCodeValue.LDC_I2_0:
                case OpCodeValue.LDC_I2_1:
                case OpCodeValue.LDC_I2_2:
                case OpCodeValue.LDC_I2_3:
                case OpCodeValue.LDC_I2_4:
                case OpCodeValue.LDC_I2_5:
                case OpCodeValue.EQL_T:
                case OpCodeValue.EQL_F:
                case OpCodeValue.EQL_NQ:
                case OpCodeValue.EQL_L:
                case OpCodeValue.EQL_LQ:
                case OpCodeValue.EQL_H:
                case OpCodeValue.EQL_HQ:
                case OpCodeValue.RET:
                case OpCodeValue.DUP:
                case OpCodeValue.POP:
                case OpCodeValue.LDLEN:
                    break;

                case OpCodeValue.LDARG_S:
                case OpCodeValue.LDLOC_S:
                case OpCodeValue.STLOC_S:
                case OpCodeValue.LDC_I4_S:
                case OpCodeValue.LDC_I2_S:
                case OpCodeValue.LDC_F4:
                case OpCodeValue.JMP:
                case OpCodeValue.JMP_T:
                case OpCodeValue.JMP_F:
                    ip++;
                    break;

                case OpCodeValue.LDC_I8_S:
                    ip += 2;
                    break;

                case OpCodeValue.CALL:
                    ip += 2;
                    break;

                case OpCodeValue.LOC_INIT:
                {
                    var locCount = *ip; ip++;
                    ip += locCount;
                    break;
                }

                default:
                    return opcode.ToString();
            }
        }

        return null;
    }
}
