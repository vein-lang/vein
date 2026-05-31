namespace ishtar_test;

using ishtar;
using ishtar.jit;
using ishtar.runtime;
using ishtar.runtime.gc;
using NUnit.Framework;
using vein.runtime;

/// <summary>
/// Direct tests for JitHelpers static methods via function pointers from JitHelpersTable.
/// These test the helper functions that are called from JIT-generated native code,
/// verifying they correctly interact with the GC, marshalling, and object model.
/// </summary>
[TestFixture]
public unsafe class JitHelpersTest : IshtarTestBase
{
    // Function pointer typedefs matching JitHelpers signatures
    private static delegate* unmanaged<RuntimeIshtarClass*, CallFrame*, IshtarObject*> _initStruct;
    private static delegate* unmanaged<IshtarObject*, RuntimeIshtarClass*, CallFrame*, IshtarObject*> _copyStruct;
    private static delegate* unmanaged<IshtarObject*, RuntimeIshtarField*, stackval*, CallFrame*, void> _storeField;
    private static delegate* unmanaged<IshtarObject*, RuntimeIshtarField*, CallFrame*, stackval*, void> _loadField;
    private static delegate* unmanaged<stackval*, RuntimeIshtarClass*, CallFrame*, IshtarObject*> _box;
    private static delegate* unmanaged<IshtarObject*, RuntimeIshtarClass*, CallFrame*, stackval*, void> _unbox;

    [OneTimeSetUp]
    public static void InitHelpers()
    {
        VirtualMachine.static_init();
        _initStruct = (delegate* unmanaged<RuntimeIshtarClass*, CallFrame*, IshtarObject*>)JitHelpers.Table.InitStruct;
        _copyStruct = (delegate* unmanaged<IshtarObject*, RuntimeIshtarClass*, CallFrame*, IshtarObject*>)JitHelpers.Table.CopyStruct;
        _storeField = (delegate* unmanaged<IshtarObject*, RuntimeIshtarField*, stackval*, CallFrame*, void>)JitHelpers.Table.StoreField;
        _loadField = (delegate* unmanaged<IshtarObject*, RuntimeIshtarField*, CallFrame*, stackval*, void>)JitHelpers.Table.LoadField;
        _box = (delegate* unmanaged<stackval*, RuntimeIshtarClass*, CallFrame*, IshtarObject*>)JitHelpers.Table.Box;
        _unbox = (delegate* unmanaged<IshtarObject*, RuntimeIshtarClass*, CallFrame*, stackval*, void>)JitHelpers.Table.Unbox;
    }
    // ═══════════════════════════════════════════════════════════════════
    // Helper_InitStruct: allocates a valid IshtarObject with correct class
    // ═══════════════════════════════════════════════════════════════════

    [Test]
    [Parallelizable(ParallelScope.None)]
    public void InitStruct_AllocatesCorrectClass()
    {
        using var scope = CreateScope();

        scope.OnClassBuild((@class, ctx_inner) =>
        {
            var structClass = scope.Module.DefineClass(new NameSymbol("AllocClass"), NamespaceSymbol.Internal);
            structClass.Flags = ClassFlags.Public | ClassFlags.Struct;
            structClass.DefineField("x", FieldFlags.Public, VeinTypeCode.TYPE_I4.AsClass()(scope.Types));
            ctx_inner.structClass = structClass;
        });

        scope.OnCodeBuild((gen, ctx_inner) =>
        {
            gen.Emit(OpCodes.INITSTRUCT, (VeinClass)ctx_inner.structClass);
            gen.Emit(OpCodes.RET);
        });

        var ctx = scope.Compile();
        ctx.Execute().Validate();

        var obj = (IshtarObject*)ctx.entryPointFrame->returnValue[0].data.p;
        Assert.That(obj != null, "InitStruct should return non-null object");

        // Verify the class pointer is set
        var runtimeClass = obj->clazz;
        Assert.That(runtimeClass != null, "Object class should be non-null");
        Assert.That(runtimeClass->IsStruct, Is.True, "Class should be marked as struct");
    }

    [Test]
    [Parallelizable(ParallelScope.None)]
    public void InitStruct_FieldDefaultsToZero()
    {
        using var scope = CreateScope();

        scope.OnClassBuild((@class, ctx_inner) =>
        {
            var structClass = scope.Module.DefineClass(new NameSymbol("ZeroInit"), NamespaceSymbol.Internal);
            structClass.Flags = ClassFlags.Public | ClassFlags.Struct;
            var fx = structClass.DefineField("x", FieldFlags.Public, VeinTypeCode.TYPE_I4.AsClass()(scope.Types));
            ctx_inner.structClass = structClass;
            ctx_inner.field = fx;
        });

        scope.OnCodeBuild((gen, ctx_inner) =>
        {
            VeinClass structClass = ctx_inner.structClass;
            VeinField fx = ctx_inner.field;
            gen.EnsureLocal("s", structClass);

            // INITSTRUCT then immediately load field 'x' — should be 0
            gen.Emit(OpCodes.INITSTRUCT, structClass);
            gen.Emit(OpCodes.STLOC_0);
            gen.Emit(OpCodes.LDLOC_0);
            gen.Emit(OpCodes.LDF, fx);
            gen.Emit(OpCodes.RET);
        });

        var ctx = scope.Compile();
        ctx.Execute().Validate();

        // Freshly allocated struct field should default to 0
        Equals(0, ctx.entryPointFrame->returnValue[0].data.i);
    }

    // ═══════════════════════════════════════════════════════════════════
    // Helper_CopyStruct: produces distinct object with same field values
    // ═══════════════════════════════════════════════════════════════════

    [Test]
    [Parallelizable(ParallelScope.None)]
    public void CopyStruct_ProducesDistinctObject()
    {
        using var scope = CreateScope();

        scope.OnClassBuild((@class, ctx_inner) =>
        {
            var structClass = scope.Module.DefineClass(new NameSymbol("CopyDistinct"), NamespaceSymbol.Internal);
            structClass.Flags = ClassFlags.Public | ClassFlags.Struct;
            structClass.DefineField("val", FieldFlags.Public, VeinTypeCode.TYPE_I4.AsClass()(scope.Types));
            ctx_inner.structClass = structClass;
        });

        scope.OnCodeBuild((gen, ctx_inner) =>
        {
            gen.Emit(OpCodes.INITSTRUCT, (VeinClass)ctx_inner.structClass);
            gen.Emit(OpCodes.RET);
        });

        var ctx = scope.Compile();
        ctx.Execute().Validate();

        var src = (IshtarObject*)ctx.entryPointFrame->returnValue[0].data.p;
        Assert.That(src != null);

        var runtimeClass = src->clazz;
        var copy = _copyStruct(src, runtimeClass, ctx.entryPointFrame);

        Assert.That(copy != null, "Copy should be non-null");
        Assert.That(copy != src, "Copy should be a different object");
        Assert.That(copy->clazz == runtimeClass, "Copy should have same class");
    }

    // ═══════════════════════════════════════════════════════════════════
    // Helper_StoreField / Helper_LoadField: roundtrip int value
    // ═══════════════════════════════════════════════════════════════════

    [Test]
    [Parallelizable(ParallelScope.None)]
    public void StoreField_LoadField_Int32Roundtrip()
    {
        using var scope = CreateScope();

        scope.OnClassBuild((@class, ctx_inner) =>
        {
            var structClass = scope.Module.DefineClass(new NameSymbol("FieldRT"), NamespaceSymbol.Internal);
            structClass.Flags = ClassFlags.Public | ClassFlags.Struct;
            var fx = structClass.DefineField("x", FieldFlags.Public, VeinTypeCode.TYPE_I4.AsClass()(scope.Types));
            ctx_inner.structClass = structClass;
            ctx_inner.field = fx;
        });

        scope.OnCodeBuild((gen, ctx_inner) =>
        {
            gen.Emit(OpCodes.INITSTRUCT, (VeinClass)ctx_inner.structClass);
            gen.Emit(OpCodes.RET);
        });

        var ctx = scope.Compile();
        ctx.Execute().Validate();

        var obj = (IshtarObject*)ctx.entryPointFrame->returnValue[0].data.p;
        var runtimeClass = obj->clazz;
        var field = runtimeClass->FindField("x");

        Assert.That(field != null, "Should find field 'x'");

        // Store value
        var sv = new stackval { type = VeinTypeCode.TYPE_I4 };
        sv.data.i = 12345;
        _storeField(obj, field, &sv, ctx.entryPointFrame);

        // Load value
        var result = new stackval();
        _loadField(obj, field, ctx.entryPointFrame, &result);

        Assert.That(result.data.i, Is.EqualTo(12345));
    }

    // ═══════════════════════════════════════════════════════════════════
    // Helper_Box / Helper_Unbox: roundtrip
    // ═══════════════════════════════════════════════════════════════════

    [Test]
    [Parallelizable(ParallelScope.None)]
    public void Box_Unbox_Roundtrip()
    {
        using var scope = CreateScope();

        scope.OnClassBuild((@class, ctx_inner) =>
        {
            var structClass = scope.Module.DefineClass(new NameSymbol("BoxRoundtrip"), NamespaceSymbol.Internal);
            structClass.Flags = ClassFlags.Public | ClassFlags.Struct;
            structClass.DefineField("!!value", FieldFlags.Public, VeinTypeCode.TYPE_I4.AsClass()(scope.Types));
            ctx_inner.structClass = structClass;
        });

        scope.OnCodeBuild((gen, ctx_inner) =>
        {
            gen.Emit(OpCodes.INITSTRUCT, (VeinClass)ctx_inner.structClass);
            gen.Emit(OpCodes.RET);
        });

        var ctx = scope.Compile();
        ctx.Execute().Validate();

        var frame = ctx.entryPointFrame;
        var src = (IshtarObject*)frame->returnValue[0].data.p;
        var runtimeClass = src->clazz;

        // Box a value
        var sv = new stackval { type = VeinTypeCode.TYPE_I4 };
        sv.data.i = 555;
        var boxed = _box(&sv, runtimeClass, frame);

        Assert.That(boxed != null, "Boxed object should be non-null");
        Assert.That(boxed->clazz == runtimeClass, "Boxed object class should match");

        // Unbox it
        var unboxed = new stackval();
        _unbox(boxed, runtimeClass, frame, &unboxed);

        Assert.That(unboxed.data.i, Is.EqualTo(555));
    }

    // ═══════════════════════════════════════════════════════════════════
    // Helper_Unbox with null object: produces TYPE_NULL
    // ═══════════════════════════════════════════════════════════════════

    [Test]
    [Parallelizable(ParallelScope.None)]
    public void Unbox_NullObject_ProducesTypeNull()
    {
        using var scope = CreateScope();
        var ctx = scope.Compile();

        var frame = ctx.entryPointFrame;
        var clazz = ctx.VM->Types->Int32Class;

        var result = new stackval();
        _unbox(null, clazz, frame, &result);

        Assert.That(result.type, Is.EqualTo(VeinTypeCode.TYPE_NULL));
        Assert.That(result.data.p, Is.EqualTo(nint.Zero));
    }

    // ═══════════════════════════════════════════════════════════════════
    // JitHelpers.Initialize: table is populated
    // ═══════════════════════════════════════════════════════════════════

    [Test]
    public void Initialize_PopulatesAllTableEntries()
    {
        // static_init calls JitHelpers.Initialize, so table should be ready
        VirtualMachine.static_init();

        Assert.That(JitHelpers.Table.InitStruct, Is.Not.EqualTo(nint.Zero));
        Assert.That(JitHelpers.Table.CopyStruct, Is.Not.EqualTo(nint.Zero));
        Assert.That(JitHelpers.Table.StoreField, Is.Not.EqualTo(nint.Zero));
        Assert.That(JitHelpers.Table.LoadField, Is.Not.EqualTo(nint.Zero));
        Assert.That(JitHelpers.Table.Box, Is.Not.EqualTo(nint.Zero));
        Assert.That(JitHelpers.Table.Unbox, Is.Not.EqualTo(nint.Zero));
    }

    // ═══════════════════════════════════════════════════════════════════
    // Helper_StoreField with NULL value: stores null in vtable
    // ═══════════════════════════════════════════════════════════════════

    [Test]
    [Parallelizable(ParallelScope.None)]
    public void StoreField_NullValue_StoresNull()
    {
        using var scope = CreateScope();

        scope.OnClassBuild((@class, ctx_inner) =>
        {
            var structClass = scope.Module.DefineClass(new NameSymbol("NullField"), NamespaceSymbol.Internal);
            structClass.Flags = ClassFlags.Public | ClassFlags.Struct;
            structClass.DefineField("ref", FieldFlags.Public, VeinTypeCode.TYPE_OBJECT.AsClass()(scope.Types));
            ctx_inner.structClass = structClass;
        });

        scope.OnCodeBuild((gen, ctx_inner) =>
        {
            gen.Emit(OpCodes.INITSTRUCT, (VeinClass)ctx_inner.structClass);
            gen.Emit(OpCodes.RET);
        });

        var ctx = scope.Compile();
        ctx.Execute().Validate();

        var obj = (IshtarObject*)ctx.entryPointFrame->returnValue[0].data.p;
        var runtimeClass = obj->clazz;
        var field = runtimeClass->FindField("ref");

        // Store null
        var sv = new stackval { type = VeinTypeCode.TYPE_NULL };
        sv.data.p = 0;
        _storeField(obj, field, &sv, ctx.entryPointFrame);

        // Verify vtable slot is null
        Assert.That((nint)obj->vtable[field->vtable_offset], Is.EqualTo(nint.Zero));
    }
}
