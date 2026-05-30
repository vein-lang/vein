namespace ishtar_test;

using System.Runtime.InteropServices;
using System.Text;
using Iced.Intel;
using ishtar;
using ishtar.collections;
using ishtar.jit;
using NUnit.Framework;
using vein.runtime;

/// <summary>
/// Integration test: compiles Vein bytecode through the full JIT pipeline
/// (BytecodeToIRBuilder → OptimizationPipeline → X64CodeGenerator),
/// executes, validates correctness against managed reference,
/// and captures x64 disassembly.
///
/// Tests:
///   1. bubble_metric(n) = n*(n-1)/2  — the comparison count of bubble sort
///   2. expression(a,b,c,d) = ((a+b)*(c-d)+a-b+c)*d — multi-op expression
///
/// Both are written in raw Vein opcodes (uint[]).
/// </summary>
[TestFixture]
public unsafe class JitBubbleSortTest
{
    private AllocatorBlock _allocator;

    [SetUp]
    public void Setup()
    {
        _allocator = new AllocatorBlock(null,
            &NativeMemory_Free,
            &NativeMemory_Realloc,
            &NativeMemory_AllocZeroed,
            &NativeMemory_AllocZeroed);
    }

    // ═══════════════════════════════════════════════════════════════════
    // Vein bytecode: int bubble_metric(int n)
    //   LDARG_0        ; push n
    //   LDARG_0        ; push n
    //   LDC_I4_1       ; push 1
    //   SUB            ; n - 1
    //   MUL            ; n * (n-1)
    //   LDC_I4_2       ; push 2
    //   DIV            ; n*(n-1)/2
    //   RET
    // ═══════════════════════════════════════════════════════════════════

    private static readonly uint[] BubbleMetricBytecode =
    [
        (uint)OpCodeValue.LDARG_0,   // 0x06
        (uint)OpCodeValue.LDARG_0,   // 0x06
        (uint)OpCodeValue.LDC_I4_1,  // 0x18
        (uint)OpCodeValue.SUB,       // 0x02
        (uint)OpCodeValue.MUL,       // 0x03
        (uint)OpCodeValue.LDC_I4_2,  // 0x19
        (uint)OpCodeValue.DIV,       // 0x04
        (uint)OpCodeValue.RET        // 0x31
    ];

    // ═══════════════════════════════════════════════════════════════════
    // Vein bytecode: int expression(int a, int b, int c, int d)
    //   result = ((a+b)*(c-d)+a-b+c)*d
    //
    //   LDARG_0        ; a
    //   LDARG_1        ; a, b
    //   ADD            ; (a+b)
    //   LDARG_2        ; (a+b), c
    //   LDARG_3        ; (a+b), c, d
    //   SUB            ; (a+b), (c-d)
    //   MUL            ; (a+b)*(c-d)
    //   LDARG_0        ; t, a
    //   ADD            ; t+a
    //   LDARG_1        ; t+a, b
    //   SUB            ; t+a-b
    //   LDARG_2        ; t+a-b, c
    //   ADD            ; t+a-b+c
    //   LDARG_3        ; t+a-b+c, d
    //   MUL            ; (t+a-b+c)*d
    //   RET
    // ═══════════════════════════════════════════════════════════════════

    private static readonly uint[] ExpressionBytecode =
    [
        (uint)OpCodeValue.LDARG_0,   // a
        (uint)OpCodeValue.LDARG_1,   // b
        (uint)OpCodeValue.ADD,       // a+b
        (uint)OpCodeValue.LDARG_2,   // c
        (uint)OpCodeValue.LDARG_3,   // d
        (uint)OpCodeValue.SUB,       // c-d
        (uint)OpCodeValue.MUL,       // (a+b)*(c-d)
        (uint)OpCodeValue.LDARG_0,   // a
        (uint)OpCodeValue.ADD,       // +a
        (uint)OpCodeValue.LDARG_1,   // b
        (uint)OpCodeValue.SUB,       // -b
        (uint)OpCodeValue.LDARG_2,   // c
        (uint)OpCodeValue.ADD,       // +c
        (uint)OpCodeValue.LDARG_3,   // d
        (uint)OpCodeValue.MUL,       // *d
        (uint)OpCodeValue.RET
    ];

    // ═══════════════════════════════════════════════════════════════════
    // Managed reference implementations
    // ═══════════════════════════════════════════════════════════════════

    private static long BubbleMetric_Managed(long n) => n * (n - 1) / 2;

    private static long Expression_Managed(long a, long b, long c, long d)
        => ((a + b) * (c - d) + a - b + c) * d;

    // ═══════════════════════════════════════════════════════════════════
    // Test: bubble_metric bytecode → JIT → execute → validate
    // ═══════════════════════════════════════════════════════════════════

    [TestCase(10L, ExpectedResult = 45L)]
    [TestCase(100L, ExpectedResult = 4950L)]
    [TestCase(1000L, ExpectedResult = 499500L)]
    [TestCase(1L, ExpectedResult = 0L)]
    [TestCase(2L, ExpectedResult = 1L)]
    [TestCase(50L, ExpectedResult = 1225L)]
    public long BubbleMetric_FromBytecode(long n)
    {
        var fn = CompileFromBytecode(BubbleMetricBytecode, 1, IRType.I4);
        var code = X64CodeGenerator.Compile(fn);
        var compiled = (delegate* unmanaged[SuppressGCTransition]<stackval*, stackval*, void>)code;

        var args = stackalloc stackval[1];
        args[0].data.l = n;

        var result = new stackval();
        compiled(args, &result);

        IRFunction.Free(fn);

        Assert.That(result.data.l, Is.EqualTo(BubbleMetric_Managed(n)));
        return result.data.l;
    }

    // ═══════════════════════════════════════════════════════════════════
    // Test: expression bytecode → JIT → execute → validate
    // ═══════════════════════════════════════════════════════════════════

    [TestCase(10L, 3L, 7L, 2L)]
    [TestCase(1L, 2L, 3L, 4L)]
    [TestCase(100L, 50L, 25L, 10L)]
    [TestCase(-5L, 3L, 8L, -2L)]
    [TestCase(0L, 0L, 0L, 1L)]
    public void Expression_FromBytecode(long a, long b, long c, long d)
    {
        var fn = CompileFromBytecode(ExpressionBytecode, 4, IRType.I4);
        var code = X64CodeGenerator.Compile(fn);
        var compiled = (delegate* unmanaged[SuppressGCTransition]<stackval*, stackval*, void>)code;

        var args = stackalloc stackval[4];
        args[0].data.l = a;
        args[1].data.l = b;
        args[2].data.l = c;
        args[3].data.l = d;

        var result = new stackval();
        compiled(args, &result);

        IRFunction.Free(fn);

        var expected = Expression_Managed(a, b, c, d);
        Assert.That(result.data.l, Is.EqualTo(expected),
            $"f({a},{b},{c},{d}): JIT={result.data.l}, managed={expected}");
    }

    // ═══════════════════════════════════════════════════════════════════
    // Test: disassemble and fixture
    // ═══════════════════════════════════════════════════════════════════

    [Test]
    public void BubbleMetric_Disassembly_Fixture()
    {
        var fn = CompileFromBytecode(BubbleMetricBytecode, 1, IRType.I4);
        X64CodeGenerator.Compile(fn, out var machineCode);
        var disasm = Disassemble(machineCode);

        TestContext.WriteLine("╔══════════════════════════════════════════════════════════════╗");
        TestContext.WriteLine("║ Vein bytecode → JIT x64: bubble_metric(n) = n*(n-1)/2      ║");
        TestContext.WriteLine("╠══════════════════════════════════════════════════════════════╣");
        TestContext.WriteLine($"║ Machine code: {machineCode.Length} bytes                                     ║");
        TestContext.WriteLine("╚══════════════════════════════════════════════════════════════╝");
        TestContext.WriteLine();
        TestContext.WriteLine("Vein IL:");
        TestContext.WriteLine("  LDARG_0");
        TestContext.WriteLine("  LDARG_0");
        TestContext.WriteLine("  LDC_I4_1");
        TestContext.WriteLine("  SUB");
        TestContext.WriteLine("  MUL");
        TestContext.WriteLine("  LDC_I4_2");
        TestContext.WriteLine("  DIV");
        TestContext.WriteLine("  RET");
        TestContext.WriteLine();
        TestContext.WriteLine("x64 asm:");
        TestContext.WriteLine(disasm);

        // Verify structure
        Assert.That(disasm, Does.Contain("push"));
        Assert.That(disasm, Does.Contain("imul"));
        Assert.That(disasm, Does.Contain("idiv"));
        Assert.That(disasm, Does.Contain("ret"));

        // Verify correctness
        var compiled = (delegate* unmanaged[SuppressGCTransition]<stackval*, stackval*, void>)
            X64CodeGenerator.Compile(fn);
        var args = stackalloc stackval[1];
        args[0].data.l = 10;
        var result = new stackval();
        compiled(args, &result);
        Assert.That(result.data.l, Is.EqualTo(45L));

        IRFunction.Free(fn);
    }

    [Test]
    public void Expression_Disassembly_Fixture()
    {
        var fn = CompileFromBytecode(ExpressionBytecode, 4, IRType.I4);
        X64CodeGenerator.Compile(fn, out var machineCode);
        var disasm = Disassemble(machineCode);

        TestContext.WriteLine("╔══════════════════════════════════════════════════════════════╗");
        TestContext.WriteLine("║ Vein bytecode → JIT x64: ((a+b)*(c-d)+a-b+c)*d             ║");
        TestContext.WriteLine("╠══════════════════════════════════════════════════════════════╣");
        TestContext.WriteLine($"║ Machine code: {machineCode.Length} bytes                                     ║");
        TestContext.WriteLine("╚══════════════════════════════════════════════════════════════╝");
        TestContext.WriteLine();
        TestContext.WriteLine("Vein IL:");
        TestContext.WriteLine("  LDARG_0  ; a");
        TestContext.WriteLine("  LDARG_1  ; b");
        TestContext.WriteLine("  ADD      ; a+b");
        TestContext.WriteLine("  LDARG_2  ; c");
        TestContext.WriteLine("  LDARG_3  ; d");
        TestContext.WriteLine("  SUB      ; c-d");
        TestContext.WriteLine("  MUL      ; (a+b)*(c-d)");
        TestContext.WriteLine("  LDARG_0  ; a");
        TestContext.WriteLine("  ADD      ; +a");
        TestContext.WriteLine("  LDARG_1  ; b");
        TestContext.WriteLine("  SUB      ; -b");
        TestContext.WriteLine("  LDARG_2  ; c");
        TestContext.WriteLine("  ADD      ; +c");
        TestContext.WriteLine("  LDARG_3  ; d");
        TestContext.WriteLine("  MUL      ; *d");
        TestContext.WriteLine("  RET");
        TestContext.WriteLine();
        TestContext.WriteLine("x64 asm:");
        TestContext.WriteLine(disasm);

        Assert.That(disasm, Does.Contain("push"));
        Assert.That(disasm, Does.Contain("imul"));
        Assert.That(disasm, Does.Contain("add"));
        Assert.That(disasm, Does.Contain("sub"));
        Assert.That(disasm, Does.Contain("ret"));

        // Verify: f(10, 3, 7, 2) = ((10+3)*(7-2)+10-3+7)*2 = (65+14)*2 = 158
        var compiled = (delegate* unmanaged[SuppressGCTransition]<stackval*, stackval*, void>)
            X64CodeGenerator.Compile(fn);
        var args = stackalloc stackval[4];
        args[0].data.l = 10;
        args[1].data.l = 3;
        args[2].data.l = 7;
        args[3].data.l = 2;
        var result = new stackval();
        compiled(args, &result);
        Assert.That(result.data.l, Is.EqualTo(158L));

        IRFunction.Free(fn);
    }

    [Test]
    public void BubbleMetric_Optimized_Disassembly_Fixture()
    {
        var fn = CompileFromBytecode(BubbleMetricBytecode, 1, IRType.I4);

        TestContext.WriteLine("╔══════════════════════════════════════════════════════════════╗");
        TestContext.WriteLine("║ O2 optimized: bubble_metric(n) = n*(n-1)/2                  ║");
        TestContext.WriteLine("╚══════════════════════════════════════════════════════════════╝");

        OptimizationPipeline.Optimize(fn, OptLevel.O2);

        X64CodeGenerator.Compile(fn, out var machineCode);
        var disasm = Disassemble(machineCode);

        TestContext.WriteLine($"Machine code: {machineCode.Length} bytes (optimized)");
        TestContext.WriteLine();
        TestContext.WriteLine("x64 asm (O2):");
        TestContext.WriteLine(disasm);

        Assert.That(disasm, Does.Contain("ret"));

        // Still correct
        var compiled = (delegate* unmanaged[SuppressGCTransition]<stackval*, stackval*, void>)
            X64CodeGenerator.Compile(fn);
        var args = stackalloc stackval[1];
        args[0].data.l = 100;
        var result = new stackval();
        compiled(args, &result);
        Assert.That(result.data.l, Is.EqualTo(4950L));

        IRFunction.Free(fn);
    }

    // ═══════════════════════════════════════════════════════════════════
    // Helper: compile Vein bytecode through the full JIT pipeline
    // ═══════════════════════════════════════════════════════════════════

    private IRFunction* CompileFromBytecode(uint[] bytecode, int argCount, IRType argType)
    {
        fixed (uint* pCode = bytecode)
        {
            var argTypes = stackalloc IRType[argCount];
            for (var i = 0; i < argCount; i++)
                argTypes[i] = argType;

            return BytecodeToIRBuilder.Build(
                pCode, (uint)bytecode.Length,
                argCount, argTypes, argType,
                maxStack: 16, _allocator);
        }
    }

    // ═══════════════════════════════════════════════════════════════════
    // Disassembler
    // ═══════════════════════════════════════════════════════════════════

    private static string Disassemble(byte[] code)
    {
        var sb = new StringBuilder();
        var decoder = Iced.Intel.Decoder.Create(64, code, DecoderOptions.None);
        var formatter = new NasmFormatter();

        var output = new StringOutput();
        while (decoder.IP < (ulong)code.Length)
        {
            var instr = decoder.Decode();
            formatter.Format(instr, output);
            sb.AppendLine($"  {decoder.IP - (ulong)instr.Length:X4}  {output.ToStringAndReset()}");
        }

        return sb.ToString();
    }

    // ═══════════════════════════════════════════════════════════════════
    // Allocator
    // ═══════════════════════════════════════════════════════════════════

    private static void* NativeMemory_AllocZeroed(uint size, void* _)
        => NativeMemory.AllocZeroed(size);
    private static void NativeMemory_Free(void* ptr)
        => NativeMemory.Free(ptr);
    private static void* NativeMemory_Realloc(void* ptr, uint newSize)
        => NativeMemory.Realloc(ptr, newSize);
}
