namespace ishtar_test;

using ishtar;
using ishtar.collections;
using ishtar.jit;
using ishtar.runtime;
using ishtar.runtime.gc;
using NUnit.Framework;
using System;
using System.IO;
using System.Runtime.InteropServices;
using vein.fs;
using vein.runtime;

/// <summary>
/// Integration test: loads std.wll, resolves methods, and executes them through the VM.
/// Validates that JIT doesn't crash on methods with unsupported opcodes.
/// </summary>
[TestFixture]
public unsafe class JitStdLibTest
{
    private static VirtualMachine* _vm;
    private static NativeList<RuntimeIshtarModule>* _deps;
    private static RuntimeIshtarModule* _std;
    private static bool _initialized;

    [SetUp]
    public void Setup()
    {
        if (_initialized) return;
        _initialized = true;

        VirtualMachine.static_init();
        var bootCfg = VirtualMachine.readBootCfg();
        var appCfg = new AppConfig(bootCfg);
        _vm = VirtualMachine.Create("jit-stdlib-test", appCfg);
        _deps = IshtarGC.AllocateList<RuntimeIshtarModule>(null);

        var resolver = _vm->Vault.GetResolver();
        resolver.AddSearchPath(new DirectoryInfo("./"));
        _std = resolver.ResolveDep("std", new IshtarVersion(0, 0), _deps);

        Assert.That(_std != null, "Failed to load std.wll");
    }

    [Test]
    public void FibTest_ExecutesWithoutCrash()
    {
        // fib_test() calls Fib(15) which uses CALL, LDLOC, STLOC, etc.
        // The JIT must NOT attempt to compile this — it should fall back to interpreter.
        var method = _std->GetSpecialEntryPoint("fib_test() -> [std]::std::Void");

        if (method == null)
            method = _std->GetSpecialEntryPoint("fib_test");

        Assert.That(method != null, "Could not find fib_test method");

        TestContext.WriteLine($"Found: {method->Name}");
        TestContext.WriteLine($"Flags: {method->Flags}");
        TestContext.WriteLine($"ArgCount: {method->ArgLength}");
        TestContext.WriteLine($"CodeSize: {method->Header->code_size}");
        TestContext.WriteLine($"IsJitted: {method->IsJitted}");
        TestContext.WriteLine($"IsEligible: {MethodCompiler.IsEligible(method)}");

        // Verify VM is accessible through the module chain
        var vmPtr = method->Owner->Owner->vm;
        TestContext.WriteLine($"VM ptr: {(nint)vmPtr:X}");
        Assert.That(vmPtr != null, "VM pointer is null — cannot execute");
        Assert.That(vmPtr == _vm, "VM pointer mismatch");

        var args = stackalloc stackval[1];
        var frame = CallFrame.Create(method, null);
        frame->args = args;

        TestContext.WriteLine("Calling exec_method...");
        _vm->exec_method(frame);

        if (frame->exception.value != null)
        {
            var ex = frame->exception.value;
            Assert.Fail($"Exception thrown: [{ex->clazz->Name}]");
        }

        TestContext.WriteLine("fib_test() executed successfully");
    }

    [Test]
    public void Fib_IsEligibleForJit()
    {
        // Fib uses self-recursion — now supported via indirect call pattern
        var method = FindMethod("Fib");
        if (method == null)
        {
            Assert.Inconclusive("Could not find Fib method");
            return;
        }

        TestContext.WriteLine($"Found: {method->Name}");
        TestContext.WriteLine($"Flags: {method->Flags}");
        TestContext.WriteLine($"CodeSize: {method->Header->code_size}");

        var eligible = MethodCompiler.IsEligible(method);
        TestContext.WriteLine($"IsEligible: {eligible}");

        Assert.That(eligible, Is.True,
            "Fib should be eligible for JIT — self-calls use indirect call pattern");
    }

    [Test]
    public void Fib_JitCompileAndExecute()
    {
        var method = FindMethod("Fib");
        Assert.That(method != null, "Could not find Fib method");

        // Dump bytecode for debugging
        var header = method->Header;
        TestContext.WriteLine($"Code size: {header->code_size}");
        TestContext.WriteLine($"Labels count: {header->labels->Count}");
        TestContext.WriteLine("Labels:");
        for (var i = 0; i < header->labels->Count; i++)
        {
            var key = header->labels->Get(i);
            if (header->labels_map->TryGetValue(key, out var label))
                TestContext.WriteLine($"  [{i}] key={key} pos={label.pos} op={label.opcode}");
        }
        TestContext.WriteLine("Bytecode:");
        var ip = header->code;
        var codeEnd = ip + header->code_size;
        var pos = 0;
        while (ip < codeEnd)
        {
            var op = (OpCodeValue)(ushort)*ip;
            TestContext.WriteLine($"  [{pos}] {op}");
            ip++; pos++;
            switch (op)
            {
                case OpCodeValue.LDARG_S:
                case OpCodeValue.LDLOC_S:
                case OpCodeValue.STLOC_S:
                case OpCodeValue.LDC_I4_S:
                case OpCodeValue.LDC_I2_S:
                case OpCodeValue.LDC_F4:
                    TestContext.WriteLine($"       operand: {*ip}");
                    ip++; pos++;
                    break;
                case OpCodeValue.LDC_I8_S:
                case OpCodeValue.CALL:
                    TestContext.WriteLine($"       operands: {*ip}, {*(ip+1)}");
                    ip += 2; pos += 2;
                    break;
                case OpCodeValue.JMP:
                case OpCodeValue.JMP_F:
                case OpCodeValue.JMP_T:
                    TestContext.WriteLine($"       label_idx: {*ip}");
                    ip++; pos++;
                    break;
                case OpCodeValue.LOC_INIT:
                    var c = (int)*ip; ip++; pos++;
                    TestContext.WriteLine($"       count: {c}");
                    ip += c * 2; pos += c * 2;
                    break;
            }
        }

        var allocator = new AllocatorBlock(null,
            &NativeMemory_Free,
            &NativeMemory_Realloc,
            &NativeMemory_AllocZeroed,
            &NativeMemory_AllocZeroed);

        var result = MethodCompiler.TryJitCompile(method, allocator);
        Assert.That(result, Is.True, $"Fib JIT compilation failed: {method->JitRejectReasonCode}");
        Assert.That(method->IsJitted, Is.True);

        // Call the JIT-compiled Fib with various inputs
        var compiled = (delegate* unmanaged[SuppressGCTransition]<stackval*, stackval*, void>)method->PIInfo.compiled_func_ref;

        // Fib(0)=0, Fib(1)=1, Fib(2)=1, Fib(5)=5, Fib(10)=55, Fib(15)=610
        int[] inputs = [0, 1, 2, 3, 5, 10, 15];
        int[] expected = [0, 1, 1, 2, 5, 55, 610];

        for (var i = 0; i < inputs.Length; i++)
        {
            var args = stackalloc stackval[1];
            args[0].data.i = inputs[i];
            args[0].type = VeinTypeCode.TYPE_I4;

            var ret = stackalloc stackval[1];
            compiled(args, ret);

            TestContext.WriteLine($"Fib({inputs[i]}) = {ret[0].data.i} (expected {expected[i]})");
            Assert.That(ret[0].data.i, Is.EqualTo(expected[i]), $"Fib({inputs[i]}) mismatch");
        }
    }

    private static void* NativeMemory_AllocZeroed(uint size, void* _)
        => NativeMemory.AllocZeroed(size);

    private static void NativeMemory_Free(void* ptr)
        => NativeMemory.Free(ptr);

    private static void* NativeMemory_Realloc(void* ptr, uint newSize)
        => NativeMemory.Realloc(ptr, newSize);

    [Test]
    public void ArithmeticMethods_CheckEligibility()
    {
        // Check all methods in std module for new eligibility with CALL/LDLOC/STLOC support
        var classCount = _std->class_table->Count;
        var eligibleCount = 0;
        var eligibleWithCall = 0;

        for (var i = 0; i < classCount; i++)
        {
            var clazz = _std->class_table->Get(i);
            var methodCount = clazz->Methods->Count;
            for (var j = 0; j < methodCount; j++)
            {
                var m = clazz->Methods->Get(j);
                if (m->IsExtern || m->IsAbstract) continue;
                if (m->Header == null || m->Header->code == null) continue;

                var eligible = MethodCompiler.IsEligible(m);
                if (eligible)
                {
                    eligibleCount++;
                    // Check if it contains CALL opcode
                    var ip = m->Header->code;
                    var end = ip + m->Header->code_size;
                    while (ip < end)
                    {
                        var op = (OpCodeValue)(ushort)*ip;
                        ip++;
                        if (op == OpCodeValue.CALL)
                        {
                            eligibleWithCall++;
                            TestContext.WriteLine($"  JIT+CALL: {clazz->Name}::{m->Name} (codeSize={m->Header->code_size})");
                            break;
                        }
                        // skip operands
                        if (op is OpCodeValue.LDARG_S or OpCodeValue.LDLOC_S or OpCodeValue.STLOC_S
                            or OpCodeValue.LDC_I4_S or OpCodeValue.LDC_I2_S or OpCodeValue.LDC_F4
                            or OpCodeValue.JMP or OpCodeValue.JMP_T or OpCodeValue.JMP_F)
                            ip++;
                        else if (op == OpCodeValue.LDC_I8_S || op == OpCodeValue.CALL)
                            ip += 2;
                        else if (op == OpCodeValue.LOC_INIT)
                        {
                            var c = *ip; ip++;
                            ip += c;
                        }
                    }
                }
            }
        }

        TestContext.WriteLine($"Total eligible methods: {eligibleCount}");
        TestContext.WriteLine($"Eligible methods with CALL: {eligibleWithCall}");
    }

    private RuntimeIshtarMethod* FindMethod(string rawName)
    {
        // Search all classes in std module for a method with matching raw name
        var classCount = _std->class_table->Count;
        for (var i = 0; i < classCount; i++)
        {
            var clazz = _std->class_table->Get(i);
            var methodCount = clazz->Methods->Count;
            for (var j = 0; j < methodCount; j++)
            {
                var m = clazz->Methods->Get(j);
                if (m->RawName.Equals(rawName))
                    return m;
            }
        }
        return null;
    }
}
