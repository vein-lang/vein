namespace ishtar_test;

using System.Runtime.InteropServices;
using ishtar;
using ishtar.collections;
using ishtar.jit;
using NUnit.Framework;

/// <summary>
/// Tests for the JIT codegen pipeline: IR construction → optimization → X64 code generation → execution.
/// Each test manually builds a small IR function, compiles it to native code, and verifies output.
/// </summary>
public unsafe class JitCodegenTest
{
    private AllocatorBlock _allocator;

    [OneTimeSetUp]
    public static void InitGC()
    {
        // NativeMemory allocator is fine for tests — no need for Boehm
    }

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
    // Const return: int f() { return 42; }
    // ═══════════════════════════════════════════════════════════════════

    [Test]
    public void ReturnConstInt()
    {
        var argTypes = stackalloc IRType[0];
        var fn = IRFunction.Create(_allocator, 0, argTypes, IRType.I4);
        var block = fn->AddBlock();

        // const 42
        var valId = fn->AllocValue(IRType.I4, block, fn->InstructionCount);
        var instr = IRInstruction.CreateConst(0, valId, 42, IRType.I4);
        var instrId = fn->AddInstruction(instr);
        fn->AppendToBlock(block, instrId);
        fn->Values[valId].DefInstrIndex = instrId;

        // return
        var retInstr = IRInstruction.CreateReturn(0, valId);
        var retId = fn->AddInstruction(retInstr);
        fn->AppendToBlock(block, retId);

        var code = X64CodeGenerator.Compile(fn);
        Assert.That((nint)code, Is.Not.EqualTo(nint.Zero));

        var compiled = (delegate* unmanaged[SuppressGCTransition]<stackval*, stackval*, void>)code;
        var result = new stackval();
        compiled(null, &result);

        Assert.That(result.data.i, Is.EqualTo(42));
        IRFunction.Free(fn);
    }

    // ═══════════════════════════════════════════════════════════════════
    // Identity: int f(int x) { return x; }
    // ═══════════════════════════════════════════════════════════════════

    [Test]
    public void ReturnArgIdentity()
    {
        var argTypes = stackalloc IRType[1];
        argTypes[0] = IRType.I4;
        var fn = IRFunction.Create(_allocator, 1, argTypes, IRType.I4);
        var block = fn->AddBlock();

        // LoadArg 0
        var argVal = fn->AllocValue(IRType.I4, block, fn->InstructionCount);
        var loadInstr = new IRInstruction();
        loadInstr.Op = IROp.LoadArg;
        loadInstr.ResultId = argVal;
        loadInstr.Immediate = 0;
        loadInstr.OperandCount = 0;
        loadInstr.BranchTarget0 = -1;
        loadInstr.BranchTarget1 = -1;
        var loadId = fn->AddInstruction(loadInstr);
        fn->AppendToBlock(block, loadId);
        fn->Values[argVal].DefInstrIndex = loadId;

        // return arg
        var retInstr = IRInstruction.CreateReturn(0, argVal);
        var retId = fn->AddInstruction(retInstr);
        fn->AppendToBlock(block, retId);

        var code = X64CodeGenerator.Compile(fn);
        var compiled = (delegate* unmanaged[SuppressGCTransition]<stackval*, stackval*, void>)code;

        var args = stackalloc stackval[1];
        args[0].data.i = 777;
        args[0].type = VeinTypeCode.TYPE_I4;

        var result = new stackval();
        compiled(args, &result);

        Assert.That(result.data.l, Is.EqualTo(777));
        IRFunction.Free(fn);
    }

    // ═══════════════════════════════════════════════════════════════════
    // Add: int f(int a, int b) { return a + b; }
    // ═══════════════════════════════════════════════════════════════════

    [Test]
    public void AddTwoArgs()
    {
        var argTypes = stackalloc IRType[2];
        argTypes[0] = IRType.I4;
        argTypes[1] = IRType.I4;
        var fn = IRFunction.Create(_allocator, 2, argTypes, IRType.I4);
        var block = fn->AddBlock();

        // LoadArg 0
        var a = EmitLoadArg(fn, block, 0, IRType.I4);
        // LoadArg 1
        var b = EmitLoadArg(fn, block, 1, IRType.I4);

        // add a, b
        var sumVal = fn->AllocValue(IRType.I4, block, fn->InstructionCount);
        var addInstr = IRInstruction.CreateBinary(0, IROp.Add, sumVal, a, b);
        var addId = fn->AddInstruction(addInstr);
        fn->AppendToBlock(block, addId);
        fn->Values[sumVal].DefInstrIndex = addId;

        // return sum
        var retInstr = IRInstruction.CreateReturn(0, sumVal);
        fn->AppendToBlock(block, fn->AddInstruction(retInstr));

        var code = X64CodeGenerator.Compile(fn);
        var compiled = (delegate* unmanaged[SuppressGCTransition]<stackval*, stackval*, void>)code;

        var args = stackalloc stackval[2];
        args[0].data.i = 123;
        args[1].data.i = 456;

        var result = new stackval();
        compiled(args, &result);

        Assert.That(result.data.l, Is.EqualTo(579));
        IRFunction.Free(fn);
    }

    // ═══════════════════════════════════════════════════════════════════
    // Sub: int f(int a, int b) { return a - b; }
    // ═══════════════════════════════════════════════════════════════════

    [Test]
    public void SubTwoArgs()
    {
        var fn = CreateBinaryFn(IROp.Sub);
        var code = X64CodeGenerator.Compile(fn);
        var compiled = (delegate* unmanaged[SuppressGCTransition]<stackval*, stackval*, void>)code;

        var args = stackalloc stackval[2];
        args[0].data.i = 100;
        args[1].data.i = 37;

        var result = new stackval();
        compiled(args, &result);

        Assert.That(result.data.l, Is.EqualTo(63));
        IRFunction.Free(fn);
    }

    // ═══════════════════════════════════════════════════════════════════
    // Mul: int f(int a, int b) { return a * b; }
    // ═══════════════════════════════════════════════════════════════════

    [Test]
    public void MulTwoArgs()
    {
        var fn = CreateBinaryFn(IROp.Mul);
        var code = X64CodeGenerator.Compile(fn);
        var compiled = (delegate* unmanaged[SuppressGCTransition]<stackval*, stackval*, void>)code;

        var args = stackalloc stackval[2];
        args[0].data.i = 7;
        args[1].data.i = 6;

        var result = new stackval();
        compiled(args, &result);

        Assert.That(result.data.l, Is.EqualTo(42));
        IRFunction.Free(fn);
    }

    // ═══════════════════════════════════════════════════════════════════
    // Bitwise AND: int f(int a, int b) { return a & b; }
    // ═══════════════════════════════════════════════════════════════════

    [Test]
    public void BitwiseAnd()
    {
        var fn = CreateBinaryFn(IROp.And);
        var code = X64CodeGenerator.Compile(fn);
        var compiled = (delegate* unmanaged[SuppressGCTransition]<stackval*, stackval*, void>)code;

        var args = stackalloc stackval[2];
        args[0].data.i = 0xFF;
        args[1].data.i = 0x0F;

        var result = new stackval();
        compiled(args, &result);

        Assert.That(result.data.l, Is.EqualTo(0x0F));
        IRFunction.Free(fn);
    }

    // ═══════════════════════════════════════════════════════════════════
    // Bitwise OR: int f(int a, int b) { return a | b; }
    // ═══════════════════════════════════════════════════════════════════

    [Test]
    public void BitwiseOr()
    {
        var fn = CreateBinaryFn(IROp.Or);
        var code = X64CodeGenerator.Compile(fn);
        var compiled = (delegate* unmanaged[SuppressGCTransition]<stackval*, stackval*, void>)code;

        var args = stackalloc stackval[2];
        args[0].data.i = 0xF0;
        args[1].data.i = 0x0F;

        var result = new stackval();
        compiled(args, &result);

        Assert.That(result.data.l, Is.EqualTo(0xFF));
        IRFunction.Free(fn);
    }

    // ═══════════════════════════════════════════════════════════════════
    // Bitwise XOR: int f(int a, int b) { return a ^ b; }
    // ═══════════════════════════════════════════════════════════════════

    [Test]
    public void BitwiseXor()
    {
        var fn = CreateBinaryFn(IROp.Xor);
        var code = X64CodeGenerator.Compile(fn);
        var compiled = (delegate* unmanaged[SuppressGCTransition]<stackval*, stackval*, void>)code;

        var args = stackalloc stackval[2];
        args[0].data.i = 0xFF;
        args[1].data.i = 0xAA;

        var result = new stackval();
        compiled(args, &result);

        Assert.That(result.data.l, Is.EqualTo(0x55));
        IRFunction.Free(fn);
    }

    // ═══════════════════════════════════════════════════════════════════
    // Shift left: int f(int a, int b) { return a << b; }
    // ═══════════════════════════════════════════════════════════════════

    [Test]
    public void ShiftLeft()
    {
        var fn = CreateBinaryFn(IROp.Shl);
        var code = X64CodeGenerator.Compile(fn);
        var compiled = (delegate* unmanaged[SuppressGCTransition]<stackval*, stackval*, void>)code;

        var args = stackalloc stackval[2];
        args[0].data.i = 1;
        args[1].data.i = 4;

        var result = new stackval();
        compiled(args, &result);

        Assert.That(result.data.l, Is.EqualTo(16));
        IRFunction.Free(fn);
    }

    // ═══════════════════════════════════════════════════════════════════
    // Shift right: int f(int a, int b) { return a >> b; }
    // ═══════════════════════════════════════════════════════════════════

    [Test]
    public void ShiftRight()
    {
        var fn = CreateBinaryFn(IROp.Shr);
        var code = X64CodeGenerator.Compile(fn);
        var compiled = (delegate* unmanaged[SuppressGCTransition]<stackval*, stackval*, void>)code;

        var args = stackalloc stackval[2];
        args[0].data.i = 64;
        args[1].data.i = 3;

        var result = new stackval();
        compiled(args, &result);

        Assert.That(result.data.l, Is.EqualTo(8));
        IRFunction.Free(fn);
    }

    // ═══════════════════════════════════════════════════════════════════
    // Negate: int f(int a) { return -a; }
    // ═══════════════════════════════════════════════════════════════════

    [Test]
    public void Negate()
    {
        var argTypes = stackalloc IRType[1];
        argTypes[0] = IRType.I4;
        var fn = IRFunction.Create(_allocator, 1, argTypes, IRType.I4);
        var block = fn->AddBlock();

        var a = EmitLoadArg(fn, block, 0, IRType.I4);

        var negVal = fn->AllocValue(IRType.I4, block, fn->InstructionCount);
        var negInstr = IRInstruction.CreateUnary(0, IROp.Neg, negVal, a);
        var negId = fn->AddInstruction(negInstr);
        fn->AppendToBlock(block, negId);
        fn->Values[negVal].DefInstrIndex = negId;

        var retInstr = IRInstruction.CreateReturn(0, negVal);
        fn->AppendToBlock(block, fn->AddInstruction(retInstr));

        var code = X64CodeGenerator.Compile(fn);
        var compiled = (delegate* unmanaged[SuppressGCTransition]<stackval*, stackval*, void>)code;

        var args = stackalloc stackval[1];
        args[0].data.i = 99;

        var result = new stackval();
        compiled(args, &result);

        Assert.That(result.data.l, Is.EqualTo(-99));
        IRFunction.Free(fn);
    }

    // ═══════════════════════════════════════════════════════════════════
    // Compare EQ: bool f(int a, int b) { return a == b; }
    // ═══════════════════════════════════════════════════════════════════

    [Test]
    public void CompareEqual_True()
    {
        var fn = CreateBinaryFn(IROp.CmpEq, IRType.Bool);
        var code = X64CodeGenerator.Compile(fn);
        var compiled = (delegate* unmanaged[SuppressGCTransition]<stackval*, stackval*, void>)code;

        var args = stackalloc stackval[2];
        args[0].data.i = 42;
        args[1].data.i = 42;

        var result = new stackval();
        compiled(args, &result);

        Assert.That(result.data.l, Is.EqualTo(1));
        IRFunction.Free(fn);
    }

    [Test]
    public void CompareEqual_False()
    {
        var fn = CreateBinaryFn(IROp.CmpEq, IRType.Bool);
        var code = X64CodeGenerator.Compile(fn);
        var compiled = (delegate* unmanaged[SuppressGCTransition]<stackval*, stackval*, void>)code;

        var args = stackalloc stackval[2];
        args[0].data.i = 42;
        args[1].data.i = 99;

        var result = new stackval();
        compiled(args, &result);

        Assert.That(result.data.l, Is.EqualTo(0));
        IRFunction.Free(fn);
    }

    // ═══════════════════════════════════════════════════════════════════
    // Compare LT: bool f(int a, int b) { return a < b; }
    // ═══════════════════════════════════════════════════════════════════

    [Test]
    public void CompareLessThan()
    {
        var fn = CreateBinaryFn(IROp.CmpLt, IRType.Bool);
        var code = X64CodeGenerator.Compile(fn);
        var compiled = (delegate* unmanaged[SuppressGCTransition]<stackval*, stackval*, void>)code;

        var args = stackalloc stackval[2];
        args[0].data.i = 5;
        args[1].data.i = 10;

        var result = new stackval();
        compiled(args, &result);

        Assert.That(result.data.l, Is.EqualTo(1));

        // flip — should be 0
        args[0].data.i = 10;
        args[1].data.i = 5;
        compiled(args, &result);

        Assert.That(result.data.l, Is.EqualTo(0));
        IRFunction.Free(fn);
    }

    // ═══════════════════════════════════════════════════════════════════
    // Constant folding: int f() { return 3 + 4; } → should fold to 7
    // ═══════════════════════════════════════════════════════════════════

    [Test]
    public void ConstantFolding_AddTwoConsts()
    {
        var argTypes = stackalloc IRType[0];
        var fn = IRFunction.Create(_allocator, 0, argTypes, IRType.I4);
        var block = fn->AddBlock();

        // const 3
        var c3 = fn->AllocValue(IRType.I4, block, fn->InstructionCount);
        var ci3 = IRInstruction.CreateConst(0, c3, 3, IRType.I4);
        var ci3Id = fn->AddInstruction(ci3);
        fn->AppendToBlock(block, ci3Id);
        fn->Values[c3].DefInstrIndex = ci3Id;

        // const 4
        var c4 = fn->AllocValue(IRType.I4, block, fn->InstructionCount);
        var ci4 = IRInstruction.CreateConst(0, c4, 4, IRType.I4);
        var ci4Id = fn->AddInstruction(ci4);
        fn->AppendToBlock(block, ci4Id);
        fn->Values[c4].DefInstrIndex = ci4Id;

        // add
        var sum = fn->AllocValue(IRType.I4, block, fn->InstructionCount);
        var addInstr = IRInstruction.CreateBinary(0, IROp.Add, sum, c3, c4);
        var addId = fn->AddInstruction(addInstr);
        fn->AppendToBlock(block, addId);
        fn->Values[sum].DefInstrIndex = addId;

        // return
        var retInstr = IRInstruction.CreateReturn(0, sum);
        fn->AppendToBlock(block, fn->AddInstruction(retInstr));

        // Optimize with O2 (constant folding + DCE)
        OptimizationPipeline.Optimize(fn, OptLevel.O2);

        // Verify the add was folded
        var addAfter = &fn->Instructions[addId];
        Assert.That(addAfter->Op, Is.EqualTo(IROp.Const));
        Assert.That(addAfter->Immediate, Is.EqualTo(7));

        // Also compile and run to validate end-to-end
        var code = X64CodeGenerator.Compile(fn);
        var compiled = (delegate* unmanaged[SuppressGCTransition]<stackval*, stackval*, void>)code;
        var result = new stackval();
        compiled(null, &result);
        Assert.That(result.data.l, Is.EqualTo(7));

        IRFunction.Free(fn);
    }

    // ═══════════════════════════════════════════════════════════════════
    // Multi-arg: int f(int a, int b, int c) { return a + b + c; }
    // ═══════════════════════════════════════════════════════════════════

    [Test]
    public void ThreeArgAdd()
    {
        var argTypes = stackalloc IRType[3];
        argTypes[0] = IRType.I4;
        argTypes[1] = IRType.I4;
        argTypes[2] = IRType.I4;
        var fn = IRFunction.Create(_allocator, 3, argTypes, IRType.I4);
        var block = fn->AddBlock();

        var a = EmitLoadArg(fn, block, 0, IRType.I4);
        var b = EmitLoadArg(fn, block, 1, IRType.I4);
        var c = EmitLoadArg(fn, block, 2, IRType.I4);

        // t = a + b
        var t = fn->AllocValue(IRType.I4, block, fn->InstructionCount);
        var add1 = IRInstruction.CreateBinary(0, IROp.Add, t, a, b);
        var add1Id = fn->AddInstruction(add1);
        fn->AppendToBlock(block, add1Id);
        fn->Values[t].DefInstrIndex = add1Id;

        // r = t + c
        var r = fn->AllocValue(IRType.I4, block, fn->InstructionCount);
        var add2 = IRInstruction.CreateBinary(0, IROp.Add, r, t, c);
        var add2Id = fn->AddInstruction(add2);
        fn->AppendToBlock(block, add2Id);
        fn->Values[r].DefInstrIndex = add2Id;

        var retInstr = IRInstruction.CreateReturn(0, r);
        fn->AppendToBlock(block, fn->AddInstruction(retInstr));

        var code = X64CodeGenerator.Compile(fn);
        var compiled = (delegate* unmanaged[SuppressGCTransition]<stackval*, stackval*, void>)code;

        var args = stackalloc stackval[3];
        args[0].data.i = 10;
        args[1].data.i = 20;
        args[2].data.i = 30;

        var result = new stackval();
        compiled(args, &result);

        Assert.That(result.data.l, Is.EqualTo(60));
        IRFunction.Free(fn);
    }

    // ═══════════════════════════════════════════════════════════════════
    // Long return: long f(long a, long b) { return a + b; }
    // ═══════════════════════════════════════════════════════════════════

    [Test]
    public void AddTwoLongs()
    {
        var argTypes = stackalloc IRType[2];
        argTypes[0] = IRType.I8;
        argTypes[1] = IRType.I8;
        var fn = IRFunction.Create(_allocator, 2, argTypes, IRType.I8);
        var block = fn->AddBlock();

        var a = EmitLoadArg(fn, block, 0, IRType.I8);
        var b = EmitLoadArg(fn, block, 1, IRType.I8);

        var sum = fn->AllocValue(IRType.I8, block, fn->InstructionCount);
        var addInstr = IRInstruction.CreateBinary(0, IROp.Add, sum, a, b);
        var addId = fn->AddInstruction(addInstr);
        fn->AppendToBlock(block, addId);
        fn->Values[sum].DefInstrIndex = addId;

        var retInstr = IRInstruction.CreateReturn(0, sum);
        fn->AppendToBlock(block, fn->AddInstruction(retInstr));

        var code = X64CodeGenerator.Compile(fn);
        var compiled = (delegate* unmanaged[SuppressGCTransition]<stackval*, stackval*, void>)code;

        var args = stackalloc stackval[2];
        args[0].data.l = 100_000_000_000L;
        args[1].data.l = 200_000_000_000L;

        var result = new stackval();
        compiled(args, &result);

        Assert.That(result.data.l, Is.EqualTo(300_000_000_000L));
        IRFunction.Free(fn);
    }

    // ═══════════════════════════════════════════════════════════════════
    // Expression: int f(int a, int b) { return (a + b) * (a - b); }
    // ═══════════════════════════════════════════════════════════════════

    [Test]
    public void ComplexExpression()
    {
        var argTypes = stackalloc IRType[2];
        argTypes[0] = IRType.I4;
        argTypes[1] = IRType.I4;
        var fn = IRFunction.Create(_allocator, 2, argTypes, IRType.I4);
        var block = fn->AddBlock();

        var a = EmitLoadArg(fn, block, 0, IRType.I4);
        var b = EmitLoadArg(fn, block, 1, IRType.I4);

        // t1 = a + b
        var t1 = fn->AllocValue(IRType.I4, block, fn->InstructionCount);
        var instr1 = IRInstruction.CreateBinary(0, IROp.Add, t1, a, b);
        fn->AppendToBlock(block, fn->AddInstruction(instr1));
        fn->Values[t1].DefInstrIndex = fn->InstructionCount - 1;

        // t2 = a - b
        var t2 = fn->AllocValue(IRType.I4, block, fn->InstructionCount);
        var instr2 = IRInstruction.CreateBinary(0, IROp.Sub, t2, a, b);
        fn->AppendToBlock(block, fn->AddInstruction(instr2));
        fn->Values[t2].DefInstrIndex = fn->InstructionCount - 1;

        // r = t1 * t2
        var r = fn->AllocValue(IRType.I4, block, fn->InstructionCount);
        var instr3 = IRInstruction.CreateBinary(0, IROp.Mul, r, t1, t2);
        fn->AppendToBlock(block, fn->AddInstruction(instr3));
        fn->Values[r].DefInstrIndex = fn->InstructionCount - 1;

        var retInstr = IRInstruction.CreateReturn(0, r);
        fn->AppendToBlock(block, fn->AddInstruction(retInstr));

        var code = X64CodeGenerator.Compile(fn);
        var compiled = (delegate* unmanaged[SuppressGCTransition]<stackval*, stackval*, void>)code;

        var args = stackalloc stackval[2];
        args[0].data.i = 10; // a=10, b=3 → (10+3)*(10-3) = 13*7 = 91
        args[1].data.i = 3;

        var result = new stackval();
        compiled(args, &result);

        Assert.That(result.data.l, Is.EqualTo(91));
        IRFunction.Free(fn);
    }

    // ═══════════════════════════════════════════════════════════════════
    // Optimization pipeline doesn't crash on simple function
    // ═══════════════════════════════════════════════════════════════════

    [TestCase(OptLevel.None)]
    [TestCase(OptLevel.O1)]
    [TestCase(OptLevel.O2)]
    [TestCase(OptLevel.O3)]
    public void OptimizationPipeline_DoesNotCrash(OptLevel level)
    {
        var argTypes = stackalloc IRType[1];
        argTypes[0] = IRType.I4;
        var fn = IRFunction.Create(_allocator, 1, argTypes, IRType.I4);
        var block = fn->AddBlock();

        var a = EmitLoadArg(fn, block, 0, IRType.I4);

        var retInstr = IRInstruction.CreateReturn(0, a);
        fn->AppendToBlock(block, fn->AddInstruction(retInstr));

        OptimizationPipeline.Optimize(fn, level);

        // Should still produce correct code after optimization
        var code = X64CodeGenerator.Compile(fn);
        var compiled = (delegate* unmanaged[SuppressGCTransition]<stackval*, stackval*, void>)code;

        var args = stackalloc stackval[1];
        args[0].data.i = 55;

        var result = new stackval();
        compiled(args, &result);

        Assert.That(result.data.l, Is.EqualTo(55));
        IRFunction.Free(fn);
    }

    // ═══════════════════════════════════════════════════════════════════
    // Register allocator: function with many values (tests spilling)
    // ═══════════════════════════════════════════════════════════════════

    [Test]
    public void ManyValues_SpillsToStack()
    {
        // Create a function with more live values than registers to force spilling
        const int argCount = 8;
        var argTypes = stackalloc IRType[argCount];
        for (var i = 0; i < argCount; i++) argTypes[i] = IRType.I4;

        var fn = IRFunction.Create(_allocator, argCount, argTypes, IRType.I4);
        var block = fn->AddBlock();

        var argVals = stackalloc int[argCount];
        for (var i = 0; i < argCount; i++)
            argVals[i] = EmitLoadArg(fn, block, i, IRType.I4);

        // Chain adds: result = a0 + a1 + a2 + ... + a7
        var acc = argVals[0];
        for (var i = 1; i < argCount; i++)
        {
            var next = fn->AllocValue(IRType.I4, block, fn->InstructionCount);
            var addInstr = IRInstruction.CreateBinary(0, IROp.Add, next, acc, argVals[i]);
            var addId = fn->AddInstruction(addInstr);
            fn->AppendToBlock(block, addId);
            fn->Values[next].DefInstrIndex = addId;
            acc = next;
        }

        var retInstr = IRInstruction.CreateReturn(0, acc);
        fn->AppendToBlock(block, fn->AddInstruction(retInstr));

        var code = X64CodeGenerator.Compile(fn);
        var compiled = (delegate* unmanaged[SuppressGCTransition]<stackval*, stackval*, void>)code;

        var args = stackalloc stackval[argCount];
        for (var i = 0; i < argCount; i++)
        {
            args[i].data.i = i + 1;
            args[i].type = VeinTypeCode.TYPE_I4;
        }

        var result = new stackval();
        compiled(args, &result);

        // 1+2+3+4+5+6+7+8 = 36
        Assert.That(result.data.l, Is.EqualTo(36));
        IRFunction.Free(fn);
    }

    // ═══════════════════════════════════════════════════════════════════
    // Dead code elimination test
    // ═══════════════════════════════════════════════════════════════════

    [Test]
    public void DeadCodeElimination_RemovesUnusedConst()
    {
        var argTypes = stackalloc IRType[0];
        var fn = IRFunction.Create(_allocator, 0, argTypes, IRType.I4);
        var block = fn->AddBlock();

        // const 999 (dead — never used)
        var dead = fn->AllocValue(IRType.I4, block, fn->InstructionCount);
        var deadInstr = IRInstruction.CreateConst(0, dead, 999, IRType.I4);
        var deadId = fn->AddInstruction(deadInstr);
        fn->AppendToBlock(block, deadId);
        fn->Values[dead].DefInstrIndex = deadId;

        // const 42 (live)
        var live = fn->AllocValue(IRType.I4, block, fn->InstructionCount);
        var liveInstr = IRInstruction.CreateConst(0, live, 42, IRType.I4);
        var liveId = fn->AddInstruction(liveInstr);
        fn->AppendToBlock(block, liveId);
        fn->Values[live].DefInstrIndex = liveId;

        // return 42
        var retInstr = IRInstruction.CreateReturn(0, live);
        fn->AppendToBlock(block, fn->AddInstruction(retInstr));

        OptimizationPipeline.Optimize(fn, OptLevel.O1);

        // Dead const should be marked dead
        Assert.That(fn->Instructions[deadId].IsDead, Is.True);
        // Live const should NOT be dead
        Assert.That(fn->Instructions[liveId].IsDead, Is.False);

        // Still produces correct output
        var code = X64CodeGenerator.Compile(fn);
        var compiled = (delegate* unmanaged[SuppressGCTransition]<stackval*, stackval*, void>)code;
        var result = new stackval();
        compiled(null, &result);
        Assert.That(result.data.l, Is.EqualTo(42));

        IRFunction.Free(fn);
    }

    // ═══════════════════════════════════════════════════════════════════
    // Helpers
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>Creates int f(int a, int b) { return a [op] b; }</summary>
    private IRFunction* CreateBinaryFn(IROp op, IRType resultType = IRType.I4)
    {
        var argTypes = stackalloc IRType[2];
        argTypes[0] = IRType.I4;
        argTypes[1] = IRType.I4;
        var fn = IRFunction.Create(_allocator, 2, argTypes, resultType);
        var block = fn->AddBlock();

        var a = EmitLoadArg(fn, block, 0, IRType.I4);
        var b = EmitLoadArg(fn, block, 1, IRType.I4);

        var r = fn->AllocValue(resultType, block, fn->InstructionCount);
        var instr = IRInstruction.CreateBinary(0, op, r, a, b);
        var instrId = fn->AddInstruction(instr);
        fn->AppendToBlock(block, instrId);
        fn->Values[r].DefInstrIndex = instrId;

        var retInstr = IRInstruction.CreateReturn(0, r);
        fn->AppendToBlock(block, fn->AddInstruction(retInstr));

        return fn;
    }

    private static int EmitLoadArg(IRFunction* fn, int block, int argIdx, IRType type)
    {
        var valId = fn->AllocValue(type, block, fn->InstructionCount);
        var instr = new IRInstruction();
        instr.Op = IROp.LoadArg;
        instr.ResultId = valId;
        instr.Immediate = argIdx;
        instr.OperandCount = 0;
        instr.BranchTarget0 = -1;
        instr.BranchTarget1 = -1;
        var instrId = fn->AddInstruction(instr);
        fn->AppendToBlock(block, instrId);
        fn->Values[valId].DefInstrIndex = instrId;
        return valId;
    }

    // ═══════════════════════════════════════════════════════════════════
    // Allocator function pointers
    // ═══════════════════════════════════════════════════════════════════

    private static void* NativeMemory_AllocZeroed(uint size, void* _)
        => NativeMemory.AllocZeroed(size);

    private static void NativeMemory_Free(void* ptr)
        => NativeMemory.Free(ptr);

    private static void* NativeMemory_Realloc(void* ptr, uint newSize)
        => NativeMemory.Realloc(ptr, newSize);
}
