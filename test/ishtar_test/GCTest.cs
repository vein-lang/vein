namespace ishtar_test;

using ishtar;
using NUnit.Framework;
using vein.runtime;
using static ishtar.NativeExports;

[TestFixture]
[Parallelizable(ParallelScope.None)]
public class GCTest : IshtarTestBase
{
    [Test]
    [Parallelizable(ParallelScope.None)]
    public unsafe void CorrectAllocateInt()
    {
        using var ctx = CreateEmptyRuntime();

        var vm = ctx.VM;
        var gc = vm->gc;

        var result = gc->ToIshtarObject(1, vm->Frames->EntryPoint);
        gc->FreeObject(&result, vm->Frames->EntryPoint);
    }

    [Test]
    [Parallelizable(ParallelScope.None)]
    public unsafe void CorrectAllocateObject()
    {
        using var ctx = CreateEmptyRuntime();

        var result = ctx.VM->gc->AllocObject(VeinTypeCode.TYPE_I8.AsRuntimeClass(ctx.VM->Types), ctx.VM->Frames->EntryPoint);
        ctx.VM->gc->FreeObject(&result, ctx.VM->Frames->EntryPoint);
    }
    //[Test]
    //[Parallelizable(ParallelScope.None)]
    //public unsafe void PopulateObjectsAndShutdownVM()
    //{
    //    GC->check_memory_leak = false;
    //    var list = new List<nint>();

    //    foreach (int i in Enumerable.Range(0, 5))
    //    {
    //        list.Add((nint)GC->AllocObject(VeinTypeCode.TYPE_I8.AsRuntimeClass(Types), GetVM().Frames->EntryPoint));
    //    }

    //    foreach (IntPtr nint in list)
    //    {
    //        var o = (IshtarObject*)nint;
    //        GC->FreeObject(o, GC->VM.Frames.GarbageCollector());
    //    }

    //    //Equals("85 objects", $"{GC->Stats.alive_objects} objects");
    //    //Equals("12600 bytes", $"{GC->Stats.total_bytes_requested} bytes");

    //    this.GC->VM.Dispose();

    //    if (GC->Stats.alive_objects != 0)
    //    {
    //        Assert.Fail($"detected memory leak, alive_objects is not empty, stillExist:\n {GC->DebugGet()}");
    //    }
    //    Equals("0 bytes", $"{GC->Stats.total_bytes_requested} bytes");
    //}

    [Test]
    [Parallelizable(ParallelScope.None)]
    public unsafe void AllocateAndFreeObject()
    {
        using var ctx = CreateEmptyRuntime();

        var vm = ctx.VM;
        var GC = vm->gc;

        var obj = GC->AllocObject(VeinTypeCode.TYPE_I8.AsRuntimeClass(vm->Types), vm->Frames->EntryPoint);
        GC->FreeObject(obj, vm->Frames->EntryPoint);
    }

    //[Test]
    //[Parallelizable(ParallelScope.None)]
    //public unsafe void CorrectAllocateArray()
    //{
    //    var array = GC->AllocArray(VeinTypeCode.TYPE_I4.AsRuntimeClass(Types), 10, 1, null, GetVM().Frames->EntryPoint);

    //    Equals(10UL, array->length);
    //    Equals(1UL, array->rank);

    //    foreach (var i in ..10)
    //        array->Set((uint)i, GC->ToIshtarObject(88 * i, GetVM().Frames->EntryPoint), GetVM().Frames->EntryPoint);

    //    foreach (var i in ..10)
    //    {
    //        var obj = array->Get((uint) i, GetVM().Frames->EntryPoint);
    //        Equals(i * 88, IshtarMarshal.ToDotnetInt32(obj, null));
    //    }
    //}
}
[TestFixture]
[Parallelizable(ParallelScope.None)]
public class GC3Test : IshtarTestBase
{
    [Test]
    [Parallelizable(ParallelScope.None)]
    public unsafe void CorrectAllocateValue()
    {
        using var ctx = CreateEmptyRuntime();

        var result = ctx.VM->gc->AllocValue(ctx.VM->Frames->EntryPoint);
        ctx.VM->gc->FreeValue(result);
    }
}
[TestFixture]
[Parallelizable(ParallelScope.None)]
public class GC2Test : IshtarTestBase
{
    [Test]
    [Parallelizable(ParallelScope.None)]
    public unsafe void CorrectAllocateValue()
    {
        using var scope = CreateScope();
        using var ctx = scope.Compile();

        var result = ctx.VM->gc->AllocValue(ctx.VM->Frames->EntryPoint);
        ctx.VM->gc->FreeValue(result);
    }
}
