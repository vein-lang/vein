namespace ishtar_test;

using System.Collections.Generic;
using System.Runtime.InteropServices;
using ishtar;
using ishtar.runtime;
using ishtar.runtime.gc;
using NUnit.Framework;
using vein.runtime;

/// <summary>
/// Tests for Boehm GC integration correctness:
/// allocation, deallocation, accounting, immortal objects, arrays, stacks, etc.
/// </summary>
[TestFixture]
[Parallelizable(ParallelScope.None)]
public class GCIntegrationTest : IshtarTestBase
{
    #region Basic Allocation / Free

    [Test]
    public unsafe void AllocValue_And_FreeValue_UpdatesCounters()
    {
        using var ctx = CreateEmptyRuntime();
        var vm = ctx.VM;
        var gc = vm->gc;

        var allocsBefore = gc->total_allocations;
        var bytesBefore = gc->total_bytes_requested;

        var val = gc->AllocValue(vm->Frames->EntryPoint);

        Assert.That(gc->total_allocations, Is.EqualTo(allocsBefore + 1));
        Assert.That(gc->total_bytes_requested, Is.GreaterThan(bytesBefore));

        gc->FreeValue(val);

        Assert.That(gc->total_allocations, Is.EqualTo(allocsBefore));
    }

    [Test]
    public unsafe void AllocRawValue_And_FreeRawValue_UpdatesCounters()
    {
        using var ctx = CreateEmptyRuntime();
        var vm = ctx.VM;
        var gc = vm->gc;

        var allocsBefore = gc->total_allocations;

        var val = gc->AllocRawValue(vm->Frames->EntryPoint);

        Assert.That(gc->total_allocations, Is.EqualTo(allocsBefore + 1));

        gc->FreeRawValue(val);

        Assert.That(gc->total_allocations, Is.EqualTo(allocsBefore));
    }

    [Test]
    public unsafe void AllocObject_And_FreeObject_UpdatesCounters()
    {
        using var ctx = CreateEmptyRuntime();
        var vm = ctx.VM;
        var gc = vm->gc;

        var allocsBefore = gc->total_allocations;
        var aliveBefore = gc->alive_objects;

        var obj = gc->AllocObject(VeinTypeCode.TYPE_I4.AsRuntimeClass(vm->Types), vm->Frames->EntryPoint);

        Assert.That(gc->total_allocations, Is.GreaterThan(allocsBefore));
        Assert.That(gc->alive_objects, Is.GreaterThan(aliveBefore));

        gc->FreeObject(obj, vm->Frames->EntryPoint);

        Assert.That(gc->alive_objects, Is.EqualTo(aliveBefore));
    }

    #endregion

    #region Stack Allocation

    [Test]
    public unsafe void AllocateStack_And_FreeStack_UpdatesCounters()
    {
        using var ctx = CreateEmptyRuntime();
        var vm = ctx.VM;
        var gc = vm->gc;

        var allocsBefore = gc->total_allocations;
        var bytesBefore = gc->total_bytes_requested;

        const int stackSize = 16;
        var stack = gc->AllocateStack(vm->Frames->EntryPoint, stackSize);

        Assert.That((nint)stack != 0, Is.True);
        Assert.That(gc->total_allocations, Is.EqualTo(allocsBefore + 1));
        Assert.That(gc->total_bytes_requested, Is.GreaterThan(bytesBefore));

        gc->FreeStack(vm->Frames->EntryPoint, stack, stackSize);

        Assert.That(gc->total_allocations, Is.EqualTo(allocsBefore));
    }

    #endregion

    #region Span Table Allocation

    [Test]
    public unsafe void AllocSpanTable_And_FreeSpanTable_UpdatesCounters()
    {
        using var ctx = CreateEmptyRuntime();
        var vm = ctx.VM;
        var gc = vm->gc;

        var allocsBefore = gc->total_allocations;

        var span = gc->AllocSpanTable(vm->Frames->EntryPoint, 64);

        Assert.That((nint)span != 0, Is.True);
        Assert.That(gc->total_allocations, Is.EqualTo(allocsBefore + 1));

        gc->FreeSpanTable(vm->Frames->EntryPoint, span);

        Assert.That(gc->total_allocations, Is.EqualTo(allocsBefore));
    }

    #endregion

    #region System Struct Allocation

    [Test]
    public unsafe void AllocateSystemStruct_ReturnsNonNull()
    {
        using var ctx = CreateEmptyRuntime();
        var vm = ctx.VM;
        var gc = vm->gc;

        var allocsBefore = gc->total_allocations;

        var ptr = gc->AllocateSystemStruct<long>(vm->Frames->EntryPoint);

        Assert.That((nint)ptr, Is.Not.EqualTo((nint)0));
        Assert.That(gc->total_allocations, Is.EqualTo(allocsBefore + 1));
    }

    #endregion

    #region Immortal Allocation

    [Test]
    public unsafe void AllocateImmortal_And_FreeImmortal()
    {
        using var ctx = CreateEmptyRuntime();
        var ptr = IshtarGC.AllocateImmortal<long>(null);

        Assert.That((nint)ptr, Is.Not.EqualTo((nint)0));

        *ptr = 0x12345678ABCDEF00;
        Assert.That(*ptr, Is.EqualTo(0x12345678ABCDEF00));

        IshtarGC.FreeImmortal(ptr);
    }

    [Test]
    public unsafe void AllocateImmortal_DoubleFree_Throws()
    {
        using var ctx = CreateEmptyRuntime();
        var ptr = IshtarGC.AllocateImmortal<long>(null);
        IshtarGC.FreeImmortal(ptr);

        Assert.Throws<TryingFreeAlreadyDisposedImmortalObject>(() =>
        {
            IshtarGC.FreeImmortal(ptr);
        });
    }

    [Test]
    public unsafe void AllocateImmortalRoot_And_FreeImmortalRoot()
    {
        using var ctx = CreateEmptyRuntime();
        var ptr = IshtarGC.AllocateImmortalRoot<long>();

        Assert.That((nint)ptr, Is.Not.EqualTo((nint)0));

        *ptr = 42;
        Assert.That(*ptr, Is.EqualTo(42));

        IshtarGC.FreeImmortalRoot(ptr);
    }

    [Test]
    public unsafe void AllocateImmortalArray_CorrectSize()
    {
        using var ctx = CreateEmptyRuntime();
        const int count = 10;
        var ptr = IshtarGC.AllocateImmortal<int>(count, null);

        Assert.That((nint)ptr, Is.Not.EqualTo((nint)0));

        for (int i = 0; i < count; i++)
            ptr[i] = i * 7;

        for (int i = 0; i < count; i++)
            Assert.That(ptr[i], Is.EqualTo(i * 7));

        IshtarGC.FreeImmortal(ptr);
    }

    #endregion

    #region Bytes Accounting Consistency

    [Test]
    public unsafe void TotalBytesRequested_ConsistentAfterAllocFree()
    {
        using var ctx = CreateEmptyRuntime();
        var vm = ctx.VM;
        var gc = vm->gc;

        var bytesBefore = gc->total_bytes_requested;

        var obj = gc->AllocObject(VeinTypeCode.TYPE_I4.AsRuntimeClass(vm->Types), vm->Frames->EntryPoint);
        var bytesAfterAlloc = gc->total_bytes_requested;

        Assert.That(bytesAfterAlloc, Is.GreaterThan(bytesBefore));

        gc->FreeObject(obj, vm->Frames->EntryPoint);
        var bytesAfterFree = gc->total_bytes_requested;

        Assert.That(bytesAfterFree, Is.LessThan(bytesAfterAlloc));
    }

    [Test]
    public unsafe void TotalBytesRequested_ConsistentForValues()
    {
        using var ctx = CreateEmptyRuntime();
        var vm = ctx.VM;
        var gc = vm->gc;

        var bytesBefore = gc->total_bytes_requested;

        var val = gc->AllocValue(vm->Frames->EntryPoint);
        var bytesAfterAlloc = gc->total_bytes_requested;
        Assert.That(bytesAfterAlloc, Is.GreaterThan(bytesBefore));

        gc->FreeValue(val);
        var bytesAfterFree = gc->total_bytes_requested;
        Assert.That(bytesAfterFree, Is.EqualTo(bytesBefore));
    }

    #endregion

    #region Multiple Allocations

    [Test]
    public unsafe void MultipleAllocations_And_Frees_CounterReturnsToBaseline()
    {
        using var ctx = CreateEmptyRuntime();
        var vm = ctx.VM;
        var gc = vm->gc;

        var allocsBefore = gc->total_allocations;

        const int count = 50;
        var ptrs = new nint[count];

        for (int i = 0; i < count; i++)
            ptrs[i] = (nint)gc->AllocValue(vm->Frames->EntryPoint);

        Assert.That(gc->total_allocations, Is.EqualTo(allocsBefore + count));

        for (int i = 0; i < count; i++)
            gc->FreeValue((stackval*)ptrs[i]);

        Assert.That(gc->total_allocations, Is.EqualTo(allocsBefore));
    }

    #endregion

    #region Object Validity

    [Test]
    public unsafe void AllocObject_IsValidObject()
    {
        using var ctx = CreateEmptyRuntime();
        var vm = ctx.VM;
        var gc = vm->gc;

        var obj = gc->AllocObject(VeinTypeCode.TYPE_I4.AsRuntimeClass(vm->Types), vm->Frames->EntryPoint);

        Assert.That(obj->IsValidObject(), Is.True);
        Assert.That((nint)obj->clazz != 0, Is.True);

        gc->FreeObject(obj, vm->Frames->EntryPoint);
    }

    [Test]
    public unsafe void AllocObject_ClassIsCorrect()
    {
        using var ctx = CreateEmptyRuntime();
        var vm = ctx.VM;
        var gc = vm->gc;

        var expectedClass = VeinTypeCode.TYPE_I4.AsRuntimeClass(vm->Types);
        var obj = gc->AllocObject(expectedClass, vm->Frames->EntryPoint);

        Assert.That((nint)obj->clazz == (nint)expectedClass, Is.True);

        gc->FreeObject(obj, vm->Frames->EntryPoint);
    }

    #endregion

    #region Array Allocation

    [Test]
    [Ignore("AllocArray requires full runtime with TYPE_ARRAY initialized")]
    public unsafe void AllocArray_ReturnsNonNull()
    {
        using var ctx = CreateEmptyRuntime();
        var vm = ctx.VM;
        var gc = vm->gc;

        var elementClass = VeinTypeCode.TYPE_I4.AsRuntimeClass(vm->Types);
        var arr = gc->AllocArray(elementClass, 5, 1, vm->Frames->EntryPoint);

        Assert.That((nint)arr, Is.Not.EqualTo((nint)0));
        Assert.That(gc->alive_objects, Is.GreaterThan(0UL));

        gc->FreeArray(arr, vm->Frames->EntryPoint);
    }

    [Test]
    [Ignore("AllocArray requires full runtime with TYPE_ARRAY initialized")]
    public unsafe void AllocArray_ZeroSize_ReturnsNonNull()
    {
        using var ctx = CreateEmptyRuntime();
        var vm = ctx.VM;
        var gc = vm->gc;

        var elementClass = VeinTypeCode.TYPE_I4.AsRuntimeClass(vm->Types);
        var arr = gc->AllocArray(elementClass, 0, 1, vm->Frames->EntryPoint);

        Assert.That((nint)arr, Is.Not.EqualTo((nint)0));

        gc->FreeArray(arr, vm->Frames->EntryPoint);
    }

    #endregion

    #region UnsafeAllocValueInto

    [Test]
    public unsafe void UnsafeAllocValueInto_SetsTypeCode()
    {
        using var ctx = CreateEmptyRuntime();
        var vm = ctx.VM;
        var gc = vm->gc;

        var val = gc->AllocValue(vm->Frames->EntryPoint);

        var intClass = VeinTypeCode.TYPE_I4.AsRuntimeClass(vm->Types);
        gc->UnsafeAllocValueInto(intClass, val);

        Assert.That(val->type, Is.EqualTo(VeinTypeCode.TYPE_I4));
        Assert.That(val->data.l, Is.EqualTo(0));

        gc->FreeValue(val);
    }

    #endregion

    #region GC Heap Info

    [Test]
    public unsafe void GetUsedMemorySize_ReturnsPositive()
    {
        using var ctx = CreateEmptyRuntime();
        var vm = ctx.VM;
        var gc = vm->gc;

        var used = gc->GetUsedMemorySize();
        Assert.That(used, Is.GreaterThanOrEqualTo(0));
    }

    [Test]
    public unsafe void Collect_DoesNotThrow()
    {
        using var ctx = CreateEmptyRuntime();
        var vm = ctx.VM;
        var gc = vm->gc;

        Assert.DoesNotThrow(() => gc->Collect());
    }

    #endregion

    #region Thread Registration

    [Test]
    public unsafe void IsRegisteredThread_MainThread_ReturnsTrue()
    {
        using var ctx = CreateEmptyRuntime();
        var vm = ctx.VM;
        var gc = vm->gc;

        // The main thread should be registered during VM init
        Assert.That(gc->is_registered_thread(), Is.True);
    }

    #endregion

    #region AllocateUVStruct

    [Test]
    public unsafe void AllocateUVStruct_And_FreeUVStruct()
    {
        using var ctx = CreateEmptyRuntime();
        var vm = ctx.VM;
        var gc = vm->gc;

        var allocsBefore = gc->total_allocations;

        var ptr = gc->AllocateUVStruct<long>(vm->Frames->EntryPoint);

        Assert.That((nint)ptr, Is.Not.EqualTo((nint)0));
        Assert.That(gc->total_allocations, Is.EqualTo(allocsBefore + 1));

        gc->FreeUVStruct(ptr, vm->Frames->EntryPoint);

        Assert.That(gc->total_allocations, Is.EqualTo(allocsBefore));
    }

    #endregion

    #region Marshal Integration

    [Test]
    public unsafe void ToIshtarObject_Int_RoundTrips()
    {
        using var ctx = CreateEmptyRuntime();
        var vm = ctx.VM;
        var gc = vm->gc;

        var obj = gc->ToIshtarObject(42, vm->Frames->EntryPoint);

        Assert.That((nint)obj != 0, Is.True);
        Assert.That(obj->IsValidObject(), Is.True);

        var dotnetVal = IshtarMarshal.ToDotnetInt32(obj, vm->Frames->EntryPoint);
        Assert.That(dotnetVal, Is.EqualTo(42));

        gc->FreeObject(&obj, vm->Frames->EntryPoint);
    }

    [Test]
    public unsafe void ToIshtarObject_String_RoundTrips()
    {
        using var ctx = CreateEmptyRuntime();
        var vm = ctx.VM;
        var gc = vm->gc;

        var obj = gc->ToIshtarObject("hello gc", vm->Frames->EntryPoint);

        Assert.That((nint)obj != 0, Is.True);
        Assert.That(obj->IsValidObject(), Is.True);

        var dotnetVal = IshtarMarshal.ToDotnetString(obj, vm->Frames->EntryPoint);
        Assert.That(dotnetVal, Is.EqualTo("hello gc"));

        gc->FreeObject(&obj, vm->Frames->EntryPoint);
    }

    [Test]
    public unsafe void ToIshtarObject_Bool_RoundTrips()
    {
        using var ctx = CreateEmptyRuntime();
        var vm = ctx.VM;
        var gc = vm->gc;

        var objTrue = gc->ToIshtarObject(true, vm->Frames->EntryPoint);
        var objFalse = gc->ToIshtarObject(false, vm->Frames->EntryPoint);

        Assert.That(IshtarMarshal.ToDotnetBoolean(objTrue, vm->Frames->EntryPoint), Is.True);
        Assert.That(IshtarMarshal.ToDotnetBoolean(objFalse, vm->Frames->EntryPoint), Is.False);

        gc->FreeObject(&objTrue, vm->Frames->EntryPoint);
        gc->FreeObject(&objFalse, vm->Frames->EntryPoint);
    }

    #endregion

    #region Stress / Bulk Allocations

    [Test]
    public unsafe void BulkAllocateObjects_NoCorruption()
    {
        using var ctx = CreateEmptyRuntime();
        var vm = ctx.VM;
        var gc = vm->gc;

        const int count = 100;
        var objects = new nint[count];

        for (int i = 0; i < count; i++)
        {
            objects[i] = (nint)gc->ToIshtarObject(i, vm->Frames->EntryPoint);
        }

        // Verify all objects are valid and have correct values
        for (int i = 0; i < count; i++)
        {
            var obj = (IshtarObject*)objects[i];
            Assert.That(obj->IsValidObject(), Is.True, $"Object {i} is invalid");
            var val = IshtarMarshal.ToDotnetInt32(obj, vm->Frames->EntryPoint);
            Assert.That(val, Is.EqualTo(i), $"Object {i} has wrong value");
        }

        for (int i = 0; i < count; i++)
        {
            var o = (IshtarObject*)objects[i];
            gc->FreeObject(o, vm->Frames->EntryPoint);
        }
    }

    [Test]
    public unsafe void BulkAllocateValues_NoLeak()
    {
        using var ctx = CreateEmptyRuntime();
        var vm = ctx.VM;
        var gc = vm->gc;

        var allocsBefore = gc->total_allocations;
        var bytesBefore = gc->total_bytes_requested;

        const int count = 200;
        var values = new nint[count];

        for (int i = 0; i < count; i++)
            values[i] = (nint)gc->AllocValue(vm->Frames->EntryPoint);

        Assert.That(gc->total_allocations, Is.EqualTo(allocsBefore + count));

        for (int i = 0; i < count; i++)
            gc->FreeValue((stackval*)values[i]);

        Assert.That(gc->total_allocations, Is.EqualTo(allocsBefore));
        Assert.That(gc->total_bytes_requested, Is.EqualTo(bytesBefore));
    }

    #endregion

    #region AllocVTable

    [Test]
    public unsafe void AllocVTable_ReturnsWritableMemory()
    {
        using var ctx = CreateEmptyRuntime();
        var vm = ctx.VM;
        var gc = vm->gc;

        const uint vtableSize = 8;
        var vtable = gc->AllocVTable(vtableSize);

        Assert.That((nint)vtable, Is.Not.EqualTo((nint)0));

        // Should be able to write without crashing
        for (uint i = 0; i < vtableSize; i++)
            vtable[i] = (void*)(nint)(i + 1);

        for (uint i = 0; i < vtableSize; i++)
            Assert.That((nint)vtable[i], Is.EqualTo((nint)(i + 1)));
    }

    #endregion

    #region Negative / Edge Cases

    [Test]
    public unsafe void TotalAllocations_NeverGoesNegative()
    {
        using var ctx = CreateEmptyRuntime();
        var vm = ctx.VM;
        var gc = vm->gc;

        // Allocate and free should always keep total_allocations >= 0
        var val = gc->AllocValue(vm->Frames->EntryPoint);
        gc->FreeValue(val);

        Assert.That(gc->total_allocations, Is.GreaterThanOrEqualTo(0));
        Assert.That(gc->total_bytes_requested, Is.GreaterThanOrEqualTo(0));
    }

    [Test]
    public unsafe void AllocObject_MultipleTypes_AllValid()
    {
        using var ctx = CreateEmptyRuntime();
        var vm = ctx.VM;
        var gc = vm->gc;

        var types = new[]
        {
            VeinTypeCode.TYPE_I1,
            VeinTypeCode.TYPE_I2,
            VeinTypeCode.TYPE_I4,
            VeinTypeCode.TYPE_I8,
            VeinTypeCode.TYPE_U1,
            VeinTypeCode.TYPE_U2,
            VeinTypeCode.TYPE_U4,
            VeinTypeCode.TYPE_U8
        };

        var allocated = new List<nint>();

        foreach (var type in types)
        {
            var cls = type.AsRuntimeClass(vm->Types);
            if (!cls->is_inited) continue; // skip uninitialized types

            var obj = gc->AllocObject(cls, vm->Frames->EntryPoint);
            Assert.That(obj->IsValidObject(), Is.True, $"Object of type {type} is invalid");
            allocated.Add((nint)obj);
        }

        Assert.That(allocated.Count, Is.GreaterThan(0), "No types were initialized");

        foreach (var ptr in allocated)
        {
            var o = (IshtarObject*)ptr;
            gc->FreeObject(o, vm->Frames->EntryPoint);
        }
    }

    #endregion

    #region Finalizers

    private static int _finalizerCallCount;

    private static void TestFinalizerCallback(nint obj, nint cd)
    {
        _finalizerCallCount++;
    }

    [Test]
    public unsafe void ObjectRegisterFinalizer_RegistersCallback()
    {
        using var ctx = CreateEmptyRuntime();
        var vm = ctx.VM;
        var gc = vm->gc;

        // AllocObject already registers _direct_finalizer.
        // We allocate, then re-register with our callback to verify registration works.
        var obj = gc->AllocObject(VeinTypeCode.TYPE_I4.AsRuntimeClass(vm->Types), vm->Frames->EntryPoint);

        _finalizerCallCount = 0;

        // Override the default finalizer with our test callback
        gc->ObjectRegisterFinalizer(obj, &TestFinalizerCallback, vm->Frames->EntryPoint);

        // FreeObject unregisters finalizer before freeing, so our callback won't be called via FreeObject.
        // But we can verify registration didn't crash and the object is still valid.
        Assert.That(obj->IsValidObject(), Is.True);

        // Clean up
        gc->FreeObject(obj, vm->Frames->EntryPoint);
    }

    [Test]
    public unsafe void ObjectRegisterFinalizer_NullUnregisters()
    {
        using var ctx = CreateEmptyRuntime();
        var vm = ctx.VM;
        var gc = vm->gc;

        var obj = gc->AllocObject(VeinTypeCode.TYPE_I4.AsRuntimeClass(vm->Types), vm->Frames->EntryPoint);

        // Unregister the default _direct_finalizer
        gc->ObjectRegisterFinalizer(obj, null, vm->Frames->EntryPoint);

        // Object should still be valid
        Assert.That(obj->IsValidObject(), Is.True);

        // FreeObject should still work even with no finalizer registered
        gc->FreeObject(obj, vm->Frames->EntryPoint);
    }

    [Test]
    public unsafe void FreeObject_DecrementsCounters_EvenWithoutFinalizer()
    {
        using var ctx = CreateEmptyRuntime();
        var vm = ctx.VM;
        var gc = vm->gc;

        var obj = gc->AllocObject(VeinTypeCode.TYPE_I4.AsRuntimeClass(vm->Types), vm->Frames->EntryPoint);

        // Unregister the finalizer
        gc->ObjectRegisterFinalizer(obj, null, vm->Frames->EntryPoint);

        var allocsBefore = gc->total_allocations;
        var aliveBefore = gc->alive_objects;

        gc->FreeObject(obj, vm->Frames->EntryPoint);

        // Counters should still be decremented by FreeObject itself
        Assert.That(gc->total_allocations, Is.LessThan(allocsBefore));
        Assert.That(gc->alive_objects, Is.LessThan(aliveBefore));
    }

    [Test]
    public unsafe void FreeObject_UnregistersFinalizer_NoDoubleFree()
    {
        using var ctx = CreateEmptyRuntime();
        var vm = ctx.VM;
        var gc = vm->gc;

        var obj = gc->AllocObject(VeinTypeCode.TYPE_I4.AsRuntimeClass(vm->Types), vm->Frames->EntryPoint);

        var allocsBefore = gc->total_allocations;

        // FreeObject unregisters the finalizer and frees
        gc->FreeObject(obj, vm->Frames->EntryPoint);

        var allocsAfter = gc->total_allocations;
        Assert.That(allocsAfter, Is.LessThan(allocsBefore));

        // Collect should not crash — finalizer was already unregistered
        gc->Collect();

        // Counters should not change further (no spurious finalization)
        Assert.That(gc->total_allocations, Is.EqualTo(allocsAfter));
    }

    [Test]
    public unsafe void Collect_AfterAllocObject_DoesNotCrash()
    {
        using var ctx = CreateEmptyRuntime();
        var vm = ctx.VM;
        var gc = vm->gc;

        // Allocate objects (they have finalizers registered via _direct_finalizer)
        var obj1 = gc->AllocObject(VeinTypeCode.TYPE_I4.AsRuntimeClass(vm->Types), vm->Frames->EntryPoint);
        var obj2 = gc->AllocObject(VeinTypeCode.TYPE_I4.AsRuntimeClass(vm->Types), vm->Frames->EntryPoint);

        // Force GC collection — should not crash even with registered finalizers
        gc->Collect();

        // Objects should still be valid (they're referenced)
        Assert.That(obj1->IsValidObject(), Is.True);
        Assert.That(obj2->IsValidObject(), Is.True);

        gc->FreeObject(obj1, vm->Frames->EntryPoint);
        gc->FreeObject(obj2, vm->Frames->EntryPoint);
    }

    [Test]
    public unsafe void SystemStructRegisterFinalizer_DoesNotCrash()
    {
        using var ctx = CreateEmptyRuntime();
        var vm = ctx.VM;
        var gc = vm->gc;

        var ptr = gc->AllocateSystemStruct<long>(vm->Frames->EntryPoint);
        *ptr = 0xDEADBEEF;

        _finalizerCallCount = 0;

        // Register a finalizer on the system struct
        gc->SystemStructRegisterFinalizer(ptr, &TestFinalizerCallback, vm->Frames->EntryPoint);

        // Verify registration didn't corrupt the data
        Assert.That(*ptr, Is.EqualTo(0xDEADBEEF));

        // Unregister before manual free
        gc->SystemStructRegisterFinalizer(ptr, null, vm->Frames->EntryPoint);
    }

    [Test]
    public unsafe void MultipleObjects_Finalization_CountersConsistent()
    {
        using var ctx = CreateEmptyRuntime();
        var vm = ctx.VM;
        var gc = vm->gc;

        var allocsBaseline = gc->total_allocations;
        var aliveBaseline = gc->alive_objects;

        const int count = 10;
        var objects = new nint[count];

        for (int i = 0; i < count; i++)
            objects[i] = (nint)gc->AllocObject(VeinTypeCode.TYPE_I4.AsRuntimeClass(vm->Types), vm->Frames->EntryPoint);

        Assert.That(gc->alive_objects, Is.EqualTo(aliveBaseline + count));

        // Free all
        for (int i = 0; i < count; i++)
            gc->FreeObject((IshtarObject*)objects[i], vm->Frames->EntryPoint);

        Assert.That(gc->alive_objects, Is.EqualTo(aliveBaseline));
        Assert.That(gc->total_allocations, Is.EqualTo(allocsBaseline));

        // Collect should not trigger any residual finalizers
        gc->Collect();

        Assert.That(gc->alive_objects, Is.EqualTo(aliveBaseline));
        Assert.That(gc->total_allocations, Is.EqualTo(allocsBaseline));
    }

    #endregion
}
