namespace ishtar_test;

using ishtar;
using ishtar.jit;
using ishtar.collections;
using ishtar.runtime;
using ishtar.runtime.gc;
using NUnit.Framework;
using System;
using System.IO;
using System.Runtime.InteropServices;
using vein.fs;
using vein.runtime;

/// <summary>
/// Tests for JIT compilation of struct-related opcodes (INITSTRUCT, CPSTRUCT, STF, LDF, BOX, UNBOX).
/// Uses the full VM pipeline: bytecode → IR → x64 code → execution via JIT.
/// Validates that struct operations work correctly when JIT-compiled rather than interpreted.
/// </summary>
[TestFixture]
public unsafe class JitStructCodegenTest : IshtarTestBase
{
    // ═══════════════════════════════════════════════════════════════════
    // INITSTRUCT → JIT: allocates struct, returns non-null pointer
    // ═══════════════════════════════════════════════════════════════════

    [Test]
    [Parallelizable(ParallelScope.None)]
    public void Jit_InitStruct_AllocatesObject()
    {
        using var scope = CreateScope();

        scope.OnClassBuild((@class, ctx) =>
        {
            var structClass = scope.Module.DefineClass(new NameSymbol("JitPoint"), NamespaceSymbol.Internal);
            structClass.Flags = ClassFlags.Public | ClassFlags.Struct;
            structClass.DefineField("x", FieldFlags.Public, VeinTypeCode.TYPE_I4.AsClass()(scope.Types));
            structClass.DefineField("y", FieldFlags.Public, VeinTypeCode.TYPE_I4.AsClass()(scope.Types));
            ctx.structClass = structClass;
        });

        scope.OnCodeBuild((gen, ctx) =>
        {
            gen.Emit(OpCodes.INITSTRUCT, (VeinClass)ctx.structClass);
            gen.Emit(OpCodes.RET);
        });

        var result = scope.Compile().Execute().Validate();
        Assert.That(result->returnValue[0].data.p, Is.Not.EqualTo((nint)0));
        Equals(VeinTypeCode.TYPE_CLASS, result->returnValue[0].type);
    }

    // ═══════════════════════════════════════════════════════════════════
    // STF + LDF → JIT: store int field, load it back
    // ═══════════════════════════════════════════════════════════════════

    [Test]
    [Parallelizable(ParallelScope.None)]
    public void Jit_StoreField_LoadField_Int32()
    {
        using var scope = CreateScope();

        scope.OnClassBuild((@class, ctx) =>
        {
            var structClass = scope.Module.DefineClass(new NameSymbol("JitVec"), NamespaceSymbol.Internal);
            structClass.Flags = ClassFlags.Public | ClassFlags.Struct;
            var fx = structClass.DefineField("x", FieldFlags.Public, VeinTypeCode.TYPE_I4.AsClass()(scope.Types));
            ctx.structClass = structClass;
            ctx.fieldX = fx;
        });

        scope.OnCodeBuild((gen, ctx) =>
        {
            VeinField fx = ctx.fieldX;
            VeinClass structClass = ctx.structClass;

            gen.EnsureLocal("s", structClass);

            gen.Emit(OpCodes.INITSTRUCT, structClass);
            gen.Emit(OpCodes.STLOC_0);

            gen.Emit(OpCodes.LDC_I4_S, 42);
            gen.Emit(OpCodes.LDLOC_0);
            gen.Emit(OpCodes.STF, fx);

            gen.Emit(OpCodes.LDLOC_0);
            gen.Emit(OpCodes.LDF, fx);

            gen.Emit(OpCodes.RET);
        });

        var result = scope.Compile().Execute().Validate();
        Equals(42, result->returnValue[0].data.i);
    }

    // ═══════════════════════════════════════════════════════════════════
    // STF + LDF → JIT: store/load I8 field
    // ═══════════════════════════════════════════════════════════════════

    [Test]
    [Parallelizable(ParallelScope.None)]
    public void Jit_StoreField_LoadField_Int64()
    {
        using var scope = CreateScope();

        scope.OnClassBuild((@class, ctx) =>
        {
            var structClass = scope.Module.DefineClass(new NameSymbol("JitLongBox"), NamespaceSymbol.Internal);
            structClass.Flags = ClassFlags.Public | ClassFlags.Struct;
            var fv = structClass.DefineField("val", FieldFlags.Public, VeinTypeCode.TYPE_I8.AsClass()(scope.Types));
            ctx.structClass = structClass;
            ctx.field = fv;
        });

        scope.OnCodeBuild((gen, ctx) =>
        {
            VeinField fv = ctx.field;
            VeinClass structClass = ctx.structClass;

            gen.EnsureLocal("s", structClass);

            gen.Emit(OpCodes.INITSTRUCT, structClass);
            gen.Emit(OpCodes.STLOC_0);

            gen.Emit(OpCodes.LDC_I8_S, 9_876_543_210L);
            gen.Emit(OpCodes.LDLOC_0);
            gen.Emit(OpCodes.STF, fv);

            gen.Emit(OpCodes.LDLOC_0);
            gen.Emit(OpCodes.LDF, fv);

            gen.Emit(OpCodes.RET);
        });

        var result = scope.Compile().Execute().Validate();
        Equals(9_876_543_210L, result->returnValue[0].data.l);
    }

    // ═══════════════════════════════════════════════════════════════════
    // CPSTRUCT → JIT: copy is independent from original
    // ═══════════════════════════════════════════════════════════════════

    [Test]
    [Parallelizable(ParallelScope.None)]
    public void Jit_CopyStruct_IndependentCopy()
    {
        using var scope = CreateScope();

        scope.OnClassBuild((@class, ctx) =>
        {
            var structClass = scope.Module.DefineClass(new NameSymbol("JitCopy"), NamespaceSymbol.Internal);
            structClass.Flags = ClassFlags.Public | ClassFlags.Struct;
            var fv = structClass.DefineField("val", FieldFlags.Public, VeinTypeCode.TYPE_I4.AsClass()(scope.Types));
            ctx.structClass = structClass;
            ctx.field = fv;
        });

        scope.OnCodeBuild((gen, ctx) =>
        {
            VeinField fval = ctx.field;
            VeinClass structClass = ctx.structClass;

            gen.EnsureLocal("original", structClass);
            gen.EnsureLocal("copy", structClass);

            // Create struct, set val = 100
            gen.Emit(OpCodes.INITSTRUCT, structClass);
            gen.Emit(OpCodes.STLOC_0);

            gen.Emit(OpCodes.LDC_I4_S, 100);
            gen.Emit(OpCodes.LDLOC_0);
            gen.Emit(OpCodes.STF, fval);

            // Copy
            gen.Emit(OpCodes.LDLOC_0);
            gen.Emit(OpCodes.CPSTRUCT, structClass);
            gen.Emit(OpCodes.STLOC_1);

            // Modify original to 200
            gen.Emit(OpCodes.LDC_I4_S, 200);
            gen.Emit(OpCodes.LDLOC_0);
            gen.Emit(OpCodes.STF, fval);

            // Load copy's value — should still be 100
            gen.Emit(OpCodes.LDLOC_1);
            gen.Emit(OpCodes.LDF, fval);

            gen.Emit(OpCodes.RET);
        });

        var result = scope.Compile().Execute().Validate();
        Equals(100, result->returnValue[0].data.i);
    }

    // ═══════════════════════════════════════════════════════════════════
    // BOX + UNBOX → JIT: roundtrip preserves value
    // ═══════════════════════════════════════════════════════════════════

    [Test]
    [Parallelizable(ParallelScope.None)]
    public void Jit_BoxUnbox_Roundtrip()
    {
        using var scope = CreateScope();

        scope.OnClassBuild((@class, ctx) =>
        {
            var structClass = scope.Module.DefineClass(new NameSymbol("JitBoxTest"), NamespaceSymbol.Internal);
            structClass.Flags = ClassFlags.Public | ClassFlags.Struct;
            structClass.DefineField("!!value", FieldFlags.Public, VeinTypeCode.TYPE_I4.AsClass()(scope.Types));
            ctx.structClass = structClass;
        });

        scope.OnCodeBuild((gen, ctx) =>
        {
            VeinClass structClass = ctx.structClass;

            gen.Emit(OpCodes.LDC_I4_S, 77);
            gen.Emit(OpCodes.BOX, structClass);
            gen.Emit(OpCodes.UNBOX, structClass);
            gen.Emit(OpCodes.RET);
        });

        var result = scope.Compile().Execute().Validate();
        Equals(77, result->returnValue[0].data.i);
    }

    // ═══════════════════════════════════════════════════════════════════
    // Multiple fields → JIT: write A, B, C then read B
    // ═══════════════════════════════════════════════════════════════════

    [Test]
    [Parallelizable(ParallelScope.None)]
    public void Jit_MultipleFields_CorrectFieldResolution()
    {
        using var scope = CreateScope();

        scope.OnClassBuild((@class, ctx) =>
        {
            var structClass = scope.Module.DefineClass(new NameSymbol("JitTriple"), NamespaceSymbol.Internal);
            structClass.Flags = ClassFlags.Public | ClassFlags.Struct;
            var fa = structClass.DefineField("a", FieldFlags.Public, VeinTypeCode.TYPE_I4.AsClass()(scope.Types));
            var fb = structClass.DefineField("b", FieldFlags.Public, VeinTypeCode.TYPE_I4.AsClass()(scope.Types));
            var fc = structClass.DefineField("c", FieldFlags.Public, VeinTypeCode.TYPE_I4.AsClass()(scope.Types));
            ctx.structClass = structClass;
            ctx.fa = fa;
            ctx.fb = fb;
            ctx.fc = fc;
        });

        scope.OnCodeBuild((gen, ctx) =>
        {
            VeinField fa = ctx.fa;
            VeinField fb = ctx.fb;
            VeinField fc = ctx.fc;
            VeinClass structClass = ctx.structClass;

            gen.EnsureLocal("s", structClass);

            gen.Emit(OpCodes.INITSTRUCT, structClass);
            gen.Emit(OpCodes.STLOC_0);

            // a = 10
            gen.Emit(OpCodes.LDC_I4_S, 10);
            gen.Emit(OpCodes.LDLOC_0);
            gen.Emit(OpCodes.STF, fa);

            // b = 20
            gen.Emit(OpCodes.LDC_I4_S, 20);
            gen.Emit(OpCodes.LDLOC_0);
            gen.Emit(OpCodes.STF, fb);

            // c = 30
            gen.Emit(OpCodes.LDC_I4_S, 30);
            gen.Emit(OpCodes.LDLOC_0);
            gen.Emit(OpCodes.STF, fc);

            // Return b
            gen.Emit(OpCodes.LDLOC_0);
            gen.Emit(OpCodes.LDF, fb);

            gen.Emit(OpCodes.RET);
        });

        var result = scope.Compile().Execute().Validate();
        Equals(20, result->returnValue[0].data.i);
    }

    // ═══════════════════════════════════════════════════════════════════
    // Field overwrite → JIT: verify last write wins
    // ═══════════════════════════════════════════════════════════════════

    [Test]
    [Parallelizable(ParallelScope.None)]
    public void Jit_FieldOverwrite_LastWriteWins()
    {
        using var scope = CreateScope();

        scope.OnClassBuild((@class, ctx) =>
        {
            var structClass = scope.Module.DefineClass(new NameSymbol("JitOverwrite"), NamespaceSymbol.Internal);
            structClass.Flags = ClassFlags.Public | ClassFlags.Struct;
            var fx = structClass.DefineField("x", FieldFlags.Public, VeinTypeCode.TYPE_I4.AsClass()(scope.Types));
            ctx.structClass = structClass;
            ctx.field = fx;
        });

        scope.OnCodeBuild((gen, ctx) =>
        {
            VeinField fx = ctx.field;
            VeinClass structClass = ctx.structClass;

            gen.EnsureLocal("s", structClass);

            gen.Emit(OpCodes.INITSTRUCT, structClass);
            gen.Emit(OpCodes.STLOC_0);

            // Write 111
            gen.Emit(OpCodes.LDC_I4_S, 111);
            gen.Emit(OpCodes.LDLOC_0);
            gen.Emit(OpCodes.STF, fx);

            // Overwrite with 222
            gen.Emit(OpCodes.LDC_I4_S, 222);
            gen.Emit(OpCodes.LDLOC_0);
            gen.Emit(OpCodes.STF, fx);

            // Overwrite with 333
            gen.Emit(OpCodes.LDC_I4_S, 333);
            gen.Emit(OpCodes.LDLOC_0);
            gen.Emit(OpCodes.STF, fx);

            gen.Emit(OpCodes.LDLOC_0);
            gen.Emit(OpCodes.LDF, fx);

            gen.Emit(OpCodes.RET);
        });

        var result = scope.Compile().Execute().Validate();
        Equals(333, result->returnValue[0].data.i);
    }

    // ═══════════════════════════════════════════════════════════════════
    // Two independent structs → JIT: verify isolation
    // ═══════════════════════════════════════════════════════════════════

    [Test]
    [Parallelizable(ParallelScope.None)]
    public void Jit_TwoStructInstances_Independent()
    {
        using var scope = CreateScope();

        scope.OnClassBuild((@class, ctx) =>
        {
            var structClass = scope.Module.DefineClass(new NameSymbol("JitPair"), NamespaceSymbol.Internal);
            structClass.Flags = ClassFlags.Public | ClassFlags.Struct;
            var fv = structClass.DefineField("val", FieldFlags.Public, VeinTypeCode.TYPE_I4.AsClass()(scope.Types));
            ctx.structClass = structClass;
            ctx.field = fv;
        });

        scope.OnCodeBuild((gen, ctx) =>
        {
            VeinField fval = ctx.field;
            VeinClass structClass = ctx.structClass;

            gen.EnsureLocal("a", structClass);
            gen.EnsureLocal("b", structClass);

            // a.val = 50
            gen.Emit(OpCodes.INITSTRUCT, structClass);
            gen.Emit(OpCodes.STLOC_0);
            gen.Emit(OpCodes.LDC_I4_S, 50);
            gen.Emit(OpCodes.LDLOC_0);
            gen.Emit(OpCodes.STF, fval);

            // b.val = 99
            gen.Emit(OpCodes.INITSTRUCT, structClass);
            gen.Emit(OpCodes.STLOC_1);
            gen.Emit(OpCodes.LDC_I4_S, 99);
            gen.Emit(OpCodes.LDLOC_1);
            gen.Emit(OpCodes.STF, fval);

            // Return a.val (should be 50, not affected by b)
            gen.Emit(OpCodes.LDLOC_0);
            gen.Emit(OpCodes.LDF, fval);

            gen.Emit(OpCodes.RET);
        });

        var result = scope.Compile().Execute().Validate();
        Equals(50, result->returnValue[0].data.i);
    }

    // ═══════════════════════════════════════════════════════════════════
    // CPSTRUCT with multiple fields → JIT
    // ═══════════════════════════════════════════════════════════════════

    [Test]
    [Parallelizable(ParallelScope.None)]
    public void Jit_CopyStruct_MultipleFields_AllCopied()
    {
        using var scope = CreateScope();

        scope.OnClassBuild((@class, ctx) =>
        {
            var structClass = scope.Module.DefineClass(new NameSymbol("JitVec2"), NamespaceSymbol.Internal);
            structClass.Flags = ClassFlags.Public | ClassFlags.Struct;
            var fx = structClass.DefineField("x", FieldFlags.Public, VeinTypeCode.TYPE_I4.AsClass()(scope.Types));
            var fy = structClass.DefineField("y", FieldFlags.Public, VeinTypeCode.TYPE_I4.AsClass()(scope.Types));
            ctx.structClass = structClass;
            ctx.fx = fx;
            ctx.fy = fy;
        });

        scope.OnCodeBuild((gen, ctx) =>
        {
            VeinField fx = ctx.fx;
            VeinField fy = ctx.fy;
            VeinClass structClass = ctx.structClass;

            gen.EnsureLocal("orig", structClass);
            gen.EnsureLocal("cp", structClass);

            gen.Emit(OpCodes.INITSTRUCT, structClass);
            gen.Emit(OpCodes.STLOC_0);

            // orig.x = 11, orig.y = 22
            gen.Emit(OpCodes.LDC_I4_S, 11);
            gen.Emit(OpCodes.LDLOC_0);
            gen.Emit(OpCodes.STF, fx);

            gen.Emit(OpCodes.LDC_I4_S, 22);
            gen.Emit(OpCodes.LDLOC_0);
            gen.Emit(OpCodes.STF, fy);

            // Copy
            gen.Emit(OpCodes.LDLOC_0);
            gen.Emit(OpCodes.CPSTRUCT, structClass);
            gen.Emit(OpCodes.STLOC_1);

            // Return copy.y
            gen.Emit(OpCodes.LDLOC_1);
            gen.Emit(OpCodes.LDF, fy);

            gen.Emit(OpCodes.RET);
        });

        var result = scope.Compile().Execute().Validate();
        Equals(22, result->returnValue[0].data.i);
    }

    // ═══════════════════════════════════════════════════════════════════
    // BOX preserves type → JIT: boxed value still typed correctly
    // ═══════════════════════════════════════════════════════════════════

    [Test]
    [Parallelizable(ParallelScope.None)]
    public void Jit_Box_PreservesType()
    {
        using var scope = CreateScope();

        scope.OnClassBuild((@class, ctx) =>
        {
            var structClass = scope.Module.DefineClass(new NameSymbol("JitBoxType"), NamespaceSymbol.Internal);
            structClass.Flags = ClassFlags.Public | ClassFlags.Struct;
            structClass.DefineField("!!value", FieldFlags.Public, VeinTypeCode.TYPE_I4.AsClass()(scope.Types));
            ctx.structClass = structClass;
        });

        scope.OnCodeBuild((gen, ctx) =>
        {
            VeinClass structClass = ctx.structClass;

            gen.Emit(OpCodes.LDC_I4_S, 42);
            gen.Emit(OpCodes.BOX, structClass);
            // BOX produces TYPE_CLASS on the stack
            gen.Emit(OpCodes.RET);
        });

        var result = scope.Compile().Execute().Validate();
        // Boxed value is an object reference
        Assert.That(result->returnValue[0].data.p, Is.Not.EqualTo((nint)0));
        Equals(VeinTypeCode.TYPE_CLASS, result->returnValue[0].type);
    }

    // ═══════════════════════════════════════════════════════════════════
    // MethodCompiler.IsEligible checks struct opcodes are accepted
    // ═══════════════════════════════════════════════════════════════════

    [Test]
    [Parallelizable(ParallelScope.None)]
    public void MethodCompiler_AcceptsStructOpcodes()
    {
        using var scope = CreateScope();

        scope.OnClassBuild((@class, ctx) =>
        {
            var structClass = scope.Module.DefineClass(new NameSymbol("EligStruct"), NamespaceSymbol.Internal);
            structClass.Flags = ClassFlags.Public | ClassFlags.Struct;
            var fx = structClass.DefineField("x", FieldFlags.Public, VeinTypeCode.TYPE_I4.AsClass()(scope.Types));
            ctx.structClass = structClass;
            ctx.field = fx;
        });

        scope.OnCodeBuild((gen, ctx) =>
        {
            VeinField fx = ctx.field;
            VeinClass structClass = ctx.structClass;

            gen.EnsureLocal("s", structClass);

            gen.Emit(OpCodes.INITSTRUCT, structClass);
            gen.Emit(OpCodes.STLOC_0);
            gen.Emit(OpCodes.LDC_I4_S, 5);
            gen.Emit(OpCodes.LDLOC_0);
            gen.Emit(OpCodes.STF, fx);
            gen.Emit(OpCodes.LDLOC_0);
            gen.Emit(OpCodes.CPSTRUCT, structClass);
            gen.Emit(OpCodes.LDF, fx);
            gen.Emit(OpCodes.BOX, structClass);
            gen.Emit(OpCodes.UNBOX, structClass);
            gen.Emit(OpCodes.RET);
        });

        var ctx = scope.Compile();

        // Find the compiled method and verify it's considered JIT-eligible
        var method = ctx.entryPointFrame->method;
        var eligible = MethodCompiler.IsEligible(method);

        TestContext.WriteLine($"Method: {method->Name}");
        TestContext.WriteLine($"IsEligible: {eligible}");
        TestContext.WriteLine($"CodeSize: {method->Header->code_size}");

        Assert.That(eligible, Is.True, "Method with struct opcodes should be JIT-eligible");
    }
}
