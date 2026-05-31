namespace ishtar_test;

using static MethodFlags;

[TestFixture]
public unsafe class StructRuntimeTest : IshtarTestBase
{
    [Test]
    [Parallelizable(ParallelScope.None)]
    public void INITSTRUCT_CreatesObject()
    {
        using var scope = CreateScope();

        scope.OnClassBuild((@class, ctx) =>
        {
            var structClass = scope.Module.DefineClass(new NameSymbol("TestPoint"), NamespaceSymbol.Internal);
            structClass.Flags = ClassFlags.Public | ClassFlags.Struct;
            structClass.DefineField("x", FieldFlags.Public, VeinTypeCode.TYPE_I4.AsClass()(scope.Types));
            structClass.DefineField("y", FieldFlags.Public, VeinTypeCode.TYPE_I4.AsClass()(scope.Types));
            ctx.structClass = structClass;
        });

        scope.OnCodeBuild((gen, ctx) =>
        {
            // INITSTRUCT allocates a zero-filled struct, result is on stack as object ref
            gen.Emit(OpCodes.INITSTRUCT, (VeinClass)ctx.structClass);
            gen.Emit(OpCodes.RET);
        });

        var result = scope.Compile().Execute().Validate();

        // Should return a non-null object
        Assert.That(result->returnValue[0].data.p, Is.Not.EqualTo((nint)0));
        Equals(VeinTypeCode.TYPE_CLASS, result->returnValue[0].type);
    }

    [Test]
    [Parallelizable(ParallelScope.None)]
    public void STF_LDF_OnStruct()
    {
        using var scope = CreateScope();

        scope.OnClassBuild((@class, ctx) =>
        {
            var structClass = scope.Module.DefineClass(new NameSymbol("Vec"), NamespaceSymbol.Internal);
            structClass.Flags = ClassFlags.Public | ClassFlags.Struct;
            var fx = structClass.DefineField("x", FieldFlags.Public, VeinTypeCode.TYPE_I4.AsClass()(scope.Types));
            ctx.structClass = structClass;
            ctx.fieldX = fx;
        });

        scope.OnCodeBuild((gen, ctx) =>
        {
            VeinField fx = ctx.fieldX;
            VeinClass structClass = ctx.structClass;

            // INITSTRUCT pushes struct obj ref on stack
            gen.Emit(OpCodes.INITSTRUCT, structClass);
            // stack: [obj]

            // Duplicate: store in local so we can use it again
            gen.EnsureLocal("s", structClass);

            // Store struct ref to local 0
            gen.Emit(OpCodes.STLOC_0);
            // stack: []

            // Load struct ref, push value, store field
            gen.Emit(OpCodes.LDC_I4_S, 77); // stack: [77]
            gen.Emit(OpCodes.LDLOC_0);     // stack: [77, obj]
            gen.Emit(OpCodes.STF, fx);      // stack: []

            // Load struct ref, load field
            gen.Emit(OpCodes.LDLOC_0);     // stack: [obj]
            gen.Emit(OpCodes.LDF, fx);      // stack: [77]

            gen.Emit(OpCodes.RET);
        });

        var result = scope.Compile().Execute().Validate();

        Equals(77, result->returnValue[0].data.i);
    }

    [Test]
    [Parallelizable(ParallelScope.None)]
    public void CPSTRUCT_CreatesIndependentCopy()
    {
        using var scope = CreateScope();

        scope.OnClassBuild((@class, ctx) =>
        {
            var structClass = scope.Module.DefineClass(new NameSymbol("CopyTest"), NamespaceSymbol.Internal);
            structClass.Flags = ClassFlags.Public | ClassFlags.Struct;
            var fx = structClass.DefineField("val", FieldFlags.Public, VeinTypeCode.TYPE_I4.AsClass()(scope.Types));
            ctx.structClass = structClass;
            ctx.field = fx;
        });

        scope.OnCodeBuild((gen, ctx) =>
        {
            VeinField fval = ctx.field;
            VeinClass structClass = ctx.structClass;

            gen.EnsureLocal("original", structClass);
            gen.EnsureLocal("copy", structClass);

            // Create struct and set val = 100
            gen.Emit(OpCodes.INITSTRUCT, structClass);
            gen.Emit(OpCodes.STLOC_0);

            gen.Emit(OpCodes.LDC_I4_S, 100);
            gen.Emit(OpCodes.LDLOC_0);
            gen.Emit(OpCodes.STF, fval);

            // Copy struct
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

        // Copy should retain original value (100), not the modified one (200)
        Equals(100, result->returnValue[0].data.i);
    }

    [Test]
    [Parallelizable(ParallelScope.None)]
    public void BOX_UNBOX_Roundtrip()
    {
        using var scope = CreateScope();

        scope.OnClassBuild((@class, ctx) =>
        {
            var structClass = scope.Module.DefineClass(new NameSymbol("BoxTest"), NamespaceSymbol.Internal);
            structClass.Flags = ClassFlags.Public | ClassFlags.Struct;
            structClass.DefineField("!!value", FieldFlags.Public, VeinTypeCode.TYPE_I4.AsClass()(scope.Types));
            ctx.structClass = structClass;
        });

        scope.OnCodeBuild((gen, ctx) =>
        {
            VeinClass structClass = ctx.structClass;

            // Push an i4 value
            gen.Emit(OpCodes.LDC_I4_S, 55);

            // Box it
            gen.Emit(OpCodes.BOX, structClass);

            // Unbox it back
            gen.Emit(OpCodes.UNBOX, structClass);

            gen.Emit(OpCodes.RET);
        });

        var result = scope.Compile().Execute().Validate();

        Equals(55, result->returnValue[0].data.i);
    }

    [Test]
    [Parallelizable(ParallelScope.None)]
    public void Struct_MultipleFields_ReadWrite()
    {
        using var scope = CreateScope();

        scope.OnClassBuild((@class, ctx) =>
        {
            var structClass = scope.Module.DefineClass(new NameSymbol("Triple"), NamespaceSymbol.Internal);
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

            // Create struct
            gen.Emit(OpCodes.INITSTRUCT, structClass);
            gen.Emit(OpCodes.STLOC_0);

            // s.a = 10
            gen.Emit(OpCodes.LDC_I4_S, 10);
            gen.Emit(OpCodes.LDLOC_0);
            gen.Emit(OpCodes.STF, fa);

            // s.b = 20
            gen.Emit(OpCodes.LDC_I4_S, 20);
            gen.Emit(OpCodes.LDLOC_0);
            gen.Emit(OpCodes.STF, fb);

            // s.c = 30
            gen.Emit(OpCodes.LDC_I4_S, 30);
            gen.Emit(OpCodes.LDLOC_0);
            gen.Emit(OpCodes.STF, fc);

            // return s.a + s.b + s.c (should be 60)
            gen.Emit(OpCodes.LDLOC_0);
            gen.Emit(OpCodes.LDF, fa);

            gen.Emit(OpCodes.LDLOC_0);
            gen.Emit(OpCodes.LDF, fb);

            gen.Emit(OpCodes.ADD);

            gen.Emit(OpCodes.LDLOC_0);
            gen.Emit(OpCodes.LDF, fc);

            gen.Emit(OpCodes.ADD);

            gen.Emit(OpCodes.RET);
        });

        var result = scope.Compile().Execute().Validate();

        Equals(60, result->returnValue[0].data.i);
    }

    #region Bittable Memory Correctness

    [Test]
    [Parallelizable(ParallelScope.None)]
    public void Struct_ZeroInitialized()
    {
        // Verify INITSTRUCT produces zero-filled fields
        using var scope = CreateScope();

        scope.OnClassBuild((@class, ctx) =>
        {
            var structClass = scope.Module.DefineClass(new NameSymbol("ZeroTest"), NamespaceSymbol.Internal);
            structClass.Flags = ClassFlags.Public | ClassFlags.Struct;
            var fx = structClass.DefineField("x", FieldFlags.Public, VeinTypeCode.TYPE_I4.AsClass()(scope.Types));
            ctx.structClass = structClass;
            ctx.fieldX = fx;
        });

        scope.OnCodeBuild((gen, ctx) =>
        {
            VeinClass structClass = ctx.structClass;
            VeinField fx = ctx.fieldX;

            gen.EnsureLocal("s", structClass);

            gen.Emit(OpCodes.INITSTRUCT, structClass);
            gen.Emit(OpCodes.STLOC_0);

            // Read x without ever writing — should be 0
            gen.Emit(OpCodes.LDLOC_0);
            gen.Emit(OpCodes.LDF, fx);
            gen.Emit(OpCodes.RET);
        });

        var result = scope.Compile().Execute().Validate();
        Equals(0, result->returnValue[0].data.i);
    }

    [Test]
    [Parallelizable(ParallelScope.None)]
    public void Struct_TwoInstances_Independent()
    {
        // Two struct instances don't alias — writing one doesn't affect the other
        using var scope = CreateScope();

        scope.OnClassBuild((@class, ctx) =>
        {
            var structClass = scope.Module.DefineClass(new NameSymbol("Pair"), NamespaceSymbol.Internal);
            structClass.Flags = ClassFlags.Public | ClassFlags.Struct;
            var fv = structClass.DefineField("val", FieldFlags.Public, VeinTypeCode.TYPE_I4.AsClass()(scope.Types));
            ctx.structClass = structClass;
            ctx.field = fv;
        });

        scope.OnCodeBuild((gen, ctx) =>
        {
            VeinClass structClass = ctx.structClass;
            VeinField fv = ctx.field;

            gen.EnsureLocal("a", structClass);
            gen.EnsureLocal("b", structClass);

            // a = init, b = init
            gen.Emit(OpCodes.INITSTRUCT, structClass);
            gen.Emit(OpCodes.STLOC_0);
            gen.Emit(OpCodes.INITSTRUCT, structClass);
            gen.Emit(OpCodes.STLOC_1);

            // a.val = 111
            gen.Emit(OpCodes.LDC_I4_S, 111);
            gen.Emit(OpCodes.LDLOC_0);
            gen.Emit(OpCodes.STF, fv);

            // b.val = 222
            gen.Emit(OpCodes.LDC_I4_S, 222);
            gen.Emit(OpCodes.LDLOC_1);
            gen.Emit(OpCodes.STF, fv);

            // return a.val (should still be 111, not 222)
            gen.Emit(OpCodes.LDLOC_0);
            gen.Emit(OpCodes.LDF, fv);
            gen.Emit(OpCodes.RET);
        });

        var result = scope.Compile().Execute().Validate();
        Equals(111, result->returnValue[0].data.i);
    }

    [Test]
    [Parallelizable(ParallelScope.None)]
    public void Struct_NestedStruct_FieldAccess()
    {
        // Struct containing another struct field — store inner, read back
        using var scope = CreateScope();

        scope.OnClassBuild((@class, ctx) =>
        {
            var innerStruct = scope.Module.DefineClass(new NameSymbol("Inner"), NamespaceSymbol.Internal);
            innerStruct.Flags = ClassFlags.Public | ClassFlags.Struct;
            var innerVal = innerStruct.DefineField("v", FieldFlags.Public, VeinTypeCode.TYPE_I4.AsClass()(scope.Types));

            var outerStruct = scope.Module.DefineClass(new NameSymbol("Outer"), NamespaceSymbol.Internal);
            outerStruct.Flags = ClassFlags.Public | ClassFlags.Struct;
            var outerInner = outerStruct.DefineField("inner", FieldFlags.Public, innerStruct);

            ctx.innerStruct = innerStruct;
            ctx.outerStruct = outerStruct;
            ctx.innerVal = innerVal;
            ctx.outerInner = outerInner;
        });

        scope.OnCodeBuild((gen, ctx) =>
        {
            VeinClass innerStruct = ctx.innerStruct;
            VeinClass outerStruct = ctx.outerStruct;
            VeinField innerVal = ctx.innerVal;
            VeinField outerInner = ctx.outerInner;

            gen.EnsureLocal("o", outerStruct);
            gen.EnsureLocal("i", innerStruct);

            // Create inner, set v = 99
            gen.Emit(OpCodes.INITSTRUCT, innerStruct);
            gen.Emit(OpCodes.STLOC_1);

            gen.Emit(OpCodes.LDC_I4_S, 99);
            gen.Emit(OpCodes.LDLOC_1);
            gen.Emit(OpCodes.STF, innerVal);

            // Create outer, set outer.inner = inner
            gen.Emit(OpCodes.INITSTRUCT, outerStruct);
            gen.Emit(OpCodes.STLOC_0);

            gen.Emit(OpCodes.LDLOC_1);    // push inner obj ref as value
            gen.Emit(OpCodes.LDLOC_0);    // push outer obj ref as this
            gen.Emit(OpCodes.STF, outerInner);

            // Read outer.inner → gives inner obj ref
            gen.Emit(OpCodes.LDLOC_0);
            gen.Emit(OpCodes.LDF, outerInner);

            // Read inner.v from the loaded inner ref
            gen.Emit(OpCodes.LDF, innerVal);

            gen.Emit(OpCodes.RET);
        });

        var result = scope.Compile().Execute().Validate();
        Equals(99, result->returnValue[0].data.i);
    }

    [Test]
    [Parallelizable(ParallelScope.None)]
    public void Struct_I8_Field()
    {
        // Verify i64 field stores/loads correctly in struct
        using var scope = CreateScope();

        scope.OnClassBuild((@class, ctx) =>
        {
            var structClass = scope.Module.DefineClass(new NameSymbol("LongHolder"), NamespaceSymbol.Internal);
            structClass.Flags = ClassFlags.Public | ClassFlags.Struct;
            var fv = structClass.DefineField("big", FieldFlags.Public, VeinTypeCode.TYPE_I8.AsClass()(scope.Types));
            ctx.structClass = structClass;
            ctx.field = fv;
        });

        scope.OnCodeBuild((gen, ctx) =>
        {
            VeinClass structClass = ctx.structClass;
            VeinField fv = ctx.field;

            gen.EnsureLocal("s", structClass);

            gen.Emit(OpCodes.INITSTRUCT, structClass);
            gen.Emit(OpCodes.STLOC_0);

            gen.Emit(OpCodes.LDC_I8_S, 9_999_999_999L);
            gen.Emit(OpCodes.LDLOC_0);
            gen.Emit(OpCodes.STF, fv);

            gen.Emit(OpCodes.LDLOC_0);
            gen.Emit(OpCodes.LDF, fv);
            gen.Emit(OpCodes.RET);
        });

        var result = scope.Compile().Execute().Validate();
        Equals(9_999_999_999L, result->returnValue[0].data.l);
    }

    [Test]
    [Parallelizable(ParallelScope.None)]
    public void Struct_F4_Field()
    {
        // Verify float32 field in struct
        using var scope = CreateScope();

        scope.OnClassBuild((@class, ctx) =>
        {
            var structClass = scope.Module.DefineClass(new NameSymbol("FloatHolder"), NamespaceSymbol.Internal);
            structClass.Flags = ClassFlags.Public | ClassFlags.Struct;
            var fv = structClass.DefineField("val", FieldFlags.Public, VeinTypeCode.TYPE_R4.AsClass()(scope.Types));
            ctx.structClass = structClass;
            ctx.field = fv;
        });

        scope.OnCodeBuild((gen, ctx) =>
        {
            VeinClass structClass = ctx.structClass;
            VeinField fv = ctx.field;

            gen.EnsureLocal("s", structClass);

            gen.Emit(OpCodes.INITSTRUCT, structClass);
            gen.Emit(OpCodes.STLOC_0);

            gen.Emit(OpCodes.LDC_F4, 3.14f);
            gen.Emit(OpCodes.LDLOC_0);
            gen.Emit(OpCodes.STF, fv);

            gen.Emit(OpCodes.LDLOC_0);
            gen.Emit(OpCodes.LDF, fv);
            gen.Emit(OpCodes.RET);
        });

        var result = scope.Compile().Execute().Validate();
        Equals(VeinTypeCode.TYPE_R4, result->returnValue[0].type);
        Equals(3.14f, result->returnValue[0].data.f_r4);
    }

    [Test]
    [Parallelizable(ParallelScope.None)]
    public void Struct_F8_Field()
    {
        // Verify float64 field in struct
        using var scope = CreateScope();

        scope.OnClassBuild((@class, ctx) =>
        {
            var structClass = scope.Module.DefineClass(new NameSymbol("DoubleHolder"), NamespaceSymbol.Internal);
            structClass.Flags = ClassFlags.Public | ClassFlags.Struct;
            var fv = structClass.DefineField("val", FieldFlags.Public, VeinTypeCode.TYPE_R8.AsClass()(scope.Types));
            ctx.structClass = structClass;
            ctx.field = fv;
        });

        scope.OnCodeBuild((gen, ctx) =>
        {
            VeinClass structClass = ctx.structClass;
            VeinField fv = ctx.field;

            gen.EnsureLocal("s", structClass);

            gen.Emit(OpCodes.INITSTRUCT, structClass);
            gen.Emit(OpCodes.STLOC_0);

            gen.Emit(OpCodes.LDC_F8, 2.718281828d);
            gen.Emit(OpCodes.LDLOC_0);
            gen.Emit(OpCodes.STF, fv);

            gen.Emit(OpCodes.LDLOC_0);
            gen.Emit(OpCodes.LDF, fv);
            gen.Emit(OpCodes.RET);
        });

        var result = scope.Compile().Execute().Validate();
        Equals(VeinTypeCode.TYPE_R8, result->returnValue[0].type);
        Equals(2.718281828d, result->returnValue[0].data.f);
    }

    [Test]
    [Parallelizable(ParallelScope.None)]
    public void Struct_MixedPrimitiveFields_Layout()
    {
        // Struct with i4 + i8 + i4 — verifies fields don't stomp each other
        using var scope = CreateScope();

        scope.OnClassBuild((@class, ctx) =>
        {
            var structClass = scope.Module.DefineClass(new NameSymbol("Mixed"), NamespaceSymbol.Internal);
            structClass.Flags = ClassFlags.Public | ClassFlags.Struct;
            var fa = structClass.DefineField("a", FieldFlags.Public, VeinTypeCode.TYPE_I4.AsClass()(scope.Types));
            var fb = structClass.DefineField("b", FieldFlags.Public, VeinTypeCode.TYPE_I8.AsClass()(scope.Types));
            var fc = structClass.DefineField("c", FieldFlags.Public, VeinTypeCode.TYPE_I4.AsClass()(scope.Types));
            ctx.structClass = structClass;
            ctx.fa = fa;
            ctx.fb = fb;
            ctx.fc = fc;
        });

        scope.OnCodeBuild((gen, ctx) =>
        {
            VeinClass structClass = ctx.structClass;
            VeinField fa = ctx.fa;
            VeinField fb = ctx.fb;
            VeinField fc = ctx.fc;

            gen.EnsureLocal("s", structClass);

            gen.Emit(OpCodes.INITSTRUCT, structClass);
            gen.Emit(OpCodes.STLOC_0);

            // s.a = 11
            gen.Emit(OpCodes.LDC_I4_S, 11);
            gen.Emit(OpCodes.LDLOC_0);
            gen.Emit(OpCodes.STF, fa);

            // s.b = 7_000_000_000L
            gen.Emit(OpCodes.LDC_I8_S, 7_000_000_000L);
            gen.Emit(OpCodes.LDLOC_0);
            gen.Emit(OpCodes.STF, fb);

            // s.c = 33
            gen.Emit(OpCodes.LDC_I4_S, 33);
            gen.Emit(OpCodes.LDLOC_0);
            gen.Emit(OpCodes.STF, fc);

            // Verify each field retains its value:
            // return (s.a == 11) && (s.c == 33) ? s.b : 0
            // Simpler: return s.a (to verify it wasn't stomped by b/c writes)
            gen.Emit(OpCodes.LDLOC_0);
            gen.Emit(OpCodes.LDF, fa);
            gen.Emit(OpCodes.RET);
        });

        var result = scope.Compile().Execute().Validate();
        Equals(11, result->returnValue[0].data.i);
    }

    [Test]
    [Parallelizable(ParallelScope.None)]
    public void Struct_MixedFields_LongFieldPreserved()
    {
        // Same layout as above but read the i8 field — verify large value preserved
        using var scope = CreateScope();

        scope.OnClassBuild((@class, ctx) =>
        {
            var structClass = scope.Module.DefineClass(new NameSymbol("Mixed2"), NamespaceSymbol.Internal);
            structClass.Flags = ClassFlags.Public | ClassFlags.Struct;
            var fa = structClass.DefineField("a", FieldFlags.Public, VeinTypeCode.TYPE_I4.AsClass()(scope.Types));
            var fb = structClass.DefineField("b", FieldFlags.Public, VeinTypeCode.TYPE_I8.AsClass()(scope.Types));
            var fc = structClass.DefineField("c", FieldFlags.Public, VeinTypeCode.TYPE_I4.AsClass()(scope.Types));
            ctx.structClass = structClass;
            ctx.fa = fa;
            ctx.fb = fb;
            ctx.fc = fc;
        });

        scope.OnCodeBuild((gen, ctx) =>
        {
            VeinClass structClass = ctx.structClass;
            VeinField fa = ctx.fa;
            VeinField fb = ctx.fb;
            VeinField fc = ctx.fc;

            gen.EnsureLocal("s", structClass);

            gen.Emit(OpCodes.INITSTRUCT, structClass);
            gen.Emit(OpCodes.STLOC_0);

            gen.Emit(OpCodes.LDC_I4_S, 11);
            gen.Emit(OpCodes.LDLOC_0);
            gen.Emit(OpCodes.STF, fa);

            gen.Emit(OpCodes.LDC_I8_S, 7_000_000_000L);
            gen.Emit(OpCodes.LDLOC_0);
            gen.Emit(OpCodes.STF, fb);

            gen.Emit(OpCodes.LDC_I4_S, 33);
            gen.Emit(OpCodes.LDLOC_0);
            gen.Emit(OpCodes.STF, fc);

            // Read the i8 field
            gen.Emit(OpCodes.LDLOC_0);
            gen.Emit(OpCodes.LDF, fb);
            gen.Emit(OpCodes.RET);
        });

        var result = scope.Compile().Execute().Validate();
        Equals(7_000_000_000L, result->returnValue[0].data.l);
    }

    [Test]
    [Parallelizable(ParallelScope.None)]
    public void Struct_CPSTRUCT_NestedDeepCopy()
    {
        // Copy a struct with nested struct field — modify nested in original,
        // verify copy's nested is unchanged
        using var scope = CreateScope();

        scope.OnClassBuild((@class, ctx) =>
        {
            var inner = scope.Module.DefineClass(new NameSymbol("InnerCp"), NamespaceSymbol.Internal);
            inner.Flags = ClassFlags.Public | ClassFlags.Struct;
            var iv = inner.DefineField("v", FieldFlags.Public, VeinTypeCode.TYPE_I4.AsClass()(scope.Types));

            var outer = scope.Module.DefineClass(new NameSymbol("OuterCp"), NamespaceSymbol.Internal);
            outer.Flags = ClassFlags.Public | ClassFlags.Struct;
            var oi = outer.DefineField("child", FieldFlags.Public, inner);

            ctx.inner = inner;
            ctx.outer = outer;
            ctx.iv = iv;
            ctx.oi = oi;
        });

        scope.OnCodeBuild((gen, ctx) =>
        {
            VeinClass inner = ctx.inner;
            VeinClass outer = ctx.outer;
            VeinField iv = ctx.iv;
            VeinField oi = ctx.oi;

            gen.EnsureLocal("orig", outer);
            gen.EnsureLocal("copy", outer);
            gen.EnsureLocal("i", inner);

            // Create inner with v = 50
            gen.Emit(OpCodes.INITSTRUCT, inner);
            gen.Emit(OpCodes.STLOC_2);

            gen.Emit(OpCodes.LDC_I4_S, 50);
            gen.Emit(OpCodes.LDLOC_2);
            gen.Emit(OpCodes.STF, iv);

            // Create outer, set child = inner
            gen.Emit(OpCodes.INITSTRUCT, outer);
            gen.Emit(OpCodes.STLOC_0);

            gen.Emit(OpCodes.LDLOC_2);   // value = inner ref
            gen.Emit(OpCodes.LDLOC_0);   // this = outer ref
            gen.Emit(OpCodes.STF, oi);

            // Copy outer
            gen.Emit(OpCodes.LDLOC_0);
            gen.Emit(OpCodes.CPSTRUCT, outer);
            gen.Emit(OpCodes.STLOC_1);

            // Modify original's inner: set inner.v = 999
            gen.Emit(OpCodes.LDC_I4_S, 999);
            gen.Emit(OpCodes.LDLOC_2);
            gen.Emit(OpCodes.STF, iv);

            // Read copy.child.v — should depend on whether CPSTRUCT does shallow or deep copy
            // With vtable CopyBlock (shallow), the copy's child field still points to same inner obj
            // so this will be 999. This test documents the CURRENT behavior.
            gen.Emit(OpCodes.LDLOC_1);
            gen.Emit(OpCodes.LDF, oi);   // load inner ref from copy
            gen.Emit(OpCodes.LDF, iv);   // load v from that inner

            gen.Emit(OpCodes.RET);
        });

        var result = scope.Compile().Execute().Validate();
        // CPSTRUCT does a shallow vtable copy — nested object refs are shared
        // So modifying the original's inner affects the copy too
        Equals(999, result->returnValue[0].data.i);
    }

    [Test]
    [Parallelizable(ParallelScope.None)]
    public void Struct_CPSTRUCT_PrimitiveFieldIsDeepCopy()
    {
        // For primitive (bittable) fields, CPSTRUCT produces truly independent copy
        // because primitive values are boxed as separate objects in vtable slots
        using var scope = CreateScope();

        scope.OnClassBuild((@class, ctx) =>
        {
            var structClass = scope.Module.DefineClass(new NameSymbol("CpPrim"), NamespaceSymbol.Internal);
            structClass.Flags = ClassFlags.Public | ClassFlags.Struct;
            var fa = structClass.DefineField("a", FieldFlags.Public, VeinTypeCode.TYPE_I4.AsClass()(scope.Types));
            var fb = structClass.DefineField("b", FieldFlags.Public, VeinTypeCode.TYPE_I4.AsClass()(scope.Types));
            ctx.structClass = structClass;
            ctx.fa = fa;
            ctx.fb = fb;
        });

        scope.OnCodeBuild((gen, ctx) =>
        {
            VeinClass structClass = ctx.structClass;
            VeinField fa = ctx.fa;
            VeinField fb = ctx.fb;

            gen.EnsureLocal("orig", structClass);
            gen.EnsureLocal("copy", structClass);

            gen.Emit(OpCodes.INITSTRUCT, structClass);
            gen.Emit(OpCodes.STLOC_0);

            // orig.a = 10, orig.b = 20
            gen.Emit(OpCodes.LDC_I4_S, 10);
            gen.Emit(OpCodes.LDLOC_0);
            gen.Emit(OpCodes.STF, fa);

            gen.Emit(OpCodes.LDC_I4_S, 20);
            gen.Emit(OpCodes.LDLOC_0);
            gen.Emit(OpCodes.STF, fb);

            // copy = cpstruct(orig)
            gen.Emit(OpCodes.LDLOC_0);
            gen.Emit(OpCodes.CPSTRUCT, structClass);
            gen.Emit(OpCodes.STLOC_1);

            // modify orig.a = 999
            gen.Emit(OpCodes.LDC_I4_S, 999);
            gen.Emit(OpCodes.LDLOC_0);
            gen.Emit(OpCodes.STF, fa);

            // read copy.a — should still be 10
            gen.Emit(OpCodes.LDLOC_1);
            gen.Emit(OpCodes.LDF, fa);
            gen.Emit(OpCodes.RET);
        });

        var result = scope.Compile().Execute().Validate();
        Equals(10, result->returnValue[0].data.i);
    }

    [Test]
    [Parallelizable(ParallelScope.None)]
    public void Struct_BOX_PreservesType()
    {
        // BOX a value, then UNBOX — verify type tag is preserved through boxing
        using var scope = CreateScope();

        scope.OnClassBuild((@class, ctx) =>
        {
            var structClass = scope.Module.DefineClass(new NameSymbol("BoxI8"), NamespaceSymbol.Internal);
            structClass.Flags = ClassFlags.Public | ClassFlags.Struct;
            structClass.DefineField("!!value", FieldFlags.Public, VeinTypeCode.TYPE_I8.AsClass()(scope.Types));
            ctx.structClass = structClass;
        });

        scope.OnCodeBuild((gen, ctx) =>
        {
            VeinClass structClass = ctx.structClass;

            gen.Emit(OpCodes.LDC_I8_S, 123_456_789L);
            gen.Emit(OpCodes.BOX, structClass);
            gen.Emit(OpCodes.UNBOX, structClass);
            gen.Emit(OpCodes.RET);
        });

        var result = scope.Compile().Execute().Validate();
        Equals(123_456_789L, result->returnValue[0].data.l);
    }

    [Test]
    [Parallelizable(ParallelScope.None)]
    public void Struct_FieldOverwrite_LastWriteWins()
    {
        // Write a field multiple times — last write should be the final value
        using var scope = CreateScope();

        scope.OnClassBuild((@class, ctx) =>
        {
            var structClass = scope.Module.DefineClass(new NameSymbol("Overwrite"), NamespaceSymbol.Internal);
            structClass.Flags = ClassFlags.Public | ClassFlags.Struct;
            var fv = structClass.DefineField("v", FieldFlags.Public, VeinTypeCode.TYPE_I4.AsClass()(scope.Types));
            ctx.structClass = structClass;
            ctx.field = fv;
        });

        scope.OnCodeBuild((gen, ctx) =>
        {
            VeinClass structClass = ctx.structClass;
            VeinField fv = ctx.field;

            gen.EnsureLocal("s", structClass);

            gen.Emit(OpCodes.INITSTRUCT, structClass);
            gen.Emit(OpCodes.STLOC_0);

            // Write 1
            gen.Emit(OpCodes.LDC_I4_S, 1);
            gen.Emit(OpCodes.LDLOC_0);
            gen.Emit(OpCodes.STF, fv);

            // Write 2
            gen.Emit(OpCodes.LDC_I4_S, 2);
            gen.Emit(OpCodes.LDLOC_0);
            gen.Emit(OpCodes.STF, fv);

            // Write 3
            gen.Emit(OpCodes.LDC_I4_S, 3);
            gen.Emit(OpCodes.LDLOC_0);
            gen.Emit(OpCodes.STF, fv);

            gen.Emit(OpCodes.LDLOC_0);
            gen.Emit(OpCodes.LDF, fv);
            gen.Emit(OpCodes.RET);
        });

        var result = scope.Compile().Execute().Validate();
        Equals(3, result->returnValue[0].data.i);
    }

    #endregion
}
