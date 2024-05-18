namespace ishtar_collections_test;

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ishtar.runtime;
using ishtar;
using ishtar.collections;

public unsafe class Tests
{
    [SetUp]
    public void Setup() => BoehmGCLayout.Native.GC_init();

    [TestCase(true, 10)]
    [TestCase(true, 100)]
    [TestCase(true, 1000)]
    [TestCase(true, 10000)]
    [TestCase(false, 10)]
    [TestCase(false, 100)]
    [TestCase(false, 1000)]
    [TestCase(false, 10000)]

    [TestCase(true, 10, true)]
    [TestCase(true, 100, true)]
    [TestCase(true, 1000, true)]
    [TestCase(true, 10000, true)]
    [TestCase(false, 10, true)]
    [TestCase(false, 100, true)]
    [TestCase(false, 1000, true)]
    [TestCase(false, 10000, true)]
    public void CreatePointerAndFIll(bool boehm, int size, bool useCollect = false)
    {
        var allocator = default(AllocatorBlock);

        if (boehm)
        {
            allocator = new AllocatorBlock
            {
                alloc = &IshtarGC_Alloc,
                alloc_primitives = &IshtarGC_AtomicAlloc,
                free = &IshtarGC_Free,
                realloc = &IshtarGC_Realloc
            };
        }
        else
        {
            allocator = new AllocatorBlock
            {
                alloc = &NativeMemory_AllocZeroed,
                alloc_primitives = &NativeMemory_AllocZeroed,
                free = &NativeMemory_Free,
                realloc = &NativeMemory_Realloc
            };
        }

        var list = NativeList<Magic>.Create(1, allocator);


        if (useCollect && boehm)
            BoehmGCLayout.Native.GC_gcollect();
        else if (useCollect)
            GC.Collect();

        foreach (int _ in Enumerable.Range(0, size)) list->Add(Magic.Create());

        var callerCount = 0;

        if (useCollect && boehm)
            BoehmGCLayout.Native.GC_gcollect();
        else if (useCollect)
            GC.Collect();

        list->ForEach(x =>
        {
            callerCount++;
            Assert.IsTrue(x->Assert());
        });

        Assert.AreEqual(size, callerCount);
        Assert.AreEqual(size, list->Count);
    }

    [TestCase(true, 10)]
    [TestCase(true, 100)]
    [TestCase(true, 1000)]
    [TestCase(true, 10000)]
    [TestCase(false, 10)]
    [TestCase(false, 100)]
    [TestCase(false, 1000)]
    [TestCase(false, 10000)]

    [TestCase(true, 10, true)]
    [TestCase(true, 100, true)]
    [TestCase(true, 1000, true)]
    [TestCase(true, 10000, true)]
    [TestCase(false, 10, true)]
    [TestCase(false, 100, true)]
    [TestCase(false, 1000, true)]
    [TestCase(false, 10000, true)]
    public void CreateAtomicAndFIll(bool boehm, int size, bool useCollect = false)
    {
        var allocator = default(AllocatorBlock);

        if (boehm)
        {
            allocator = new AllocatorBlock
            {
                alloc = &IshtarGC_Alloc,
                alloc_primitives = &IshtarGC_AtomicAlloc,
                free = &IshtarGC_Free,
                realloc = &IshtarGC_Realloc
            };
        }
        else
        {
            allocator = new AllocatorBlock
            {
                alloc = &NativeMemory_AllocZeroed,
                alloc_primitives = &NativeMemory_AllocZeroed,
                free = &NativeMemory_Free,
                realloc = &NativeMemory_Realloc
            };
        }

        var list = AtomicNativeList<Magic>.Create(1, allocator);

        if (useCollect && boehm)
            BoehmGCLayout.Native.GC_gcollect();
        else if (useCollect)
            GC.Collect();

        foreach (int _ in Enumerable.Range(0, size)) list->Add(Magic.CreateAtomic());

        var callerCount = 0;

        if (useCollect && boehm)
            BoehmGCLayout.Native.GC_gcollect();
        else if (useCollect)
            GC.Collect();

        list->ForEach((ref Magic x) => {
            callerCount++;
            Assert.IsTrue(x.Assert());
        });

        Assert.AreEqual(size, callerCount);
        Assert.AreEqual(size, list->Count);
    }


    [TestCase(true, 10)]
    [TestCase(true, 100)]
    [TestCase(true, 1000)]
    [TestCase(true, 10000)]
    [TestCase(false, 10)]
    [TestCase(false, 100)]
    [TestCase(false, 1000)]
    [TestCase(false, 10000)]

    [TestCase(true, 10, true)]
    [TestCase(true, 100, true)]
    [TestCase(true, 1000, true)]
    [TestCase(true, 10000, true)]
    [TestCase(false, 10, true)]
    [TestCase(false, 100, true)]
    [TestCase(false, 1000, true)]
    [TestCase(false, 10000, true)]
    public void CreatePointerDictionary(bool boehm, int size, bool useCollect = false)
    {
        var watch = Stopwatch.StartNew();
        var allocator = default(AllocatorBlock);

        if (boehm)
        {
            allocator = new AllocatorBlock
            {
                alloc = &IshtarGC_Alloc,
                alloc_primitives = &IshtarGC_AtomicAlloc,
                free = &IshtarGC_Free,
                realloc = &IshtarGC_Realloc
            };
        }
        else
        {
            allocator = new AllocatorBlock
            {
                alloc = &NativeMemory_AllocZeroed,
                alloc_primitives = &NativeMemory_AllocZeroed,
                free = &NativeMemory_Free,
                realloc = &NativeMemory_Realloc
            };
        }

        var dict = NativeDictionary<int, Magic>.Create(16, allocator);

        foreach (int i in Enumerable.Range(0, size)) dict->Add(i, Magic.Create());


        var callerCount = 0;

        if (useCollect && boehm)
            BoehmGCLayout.Native.GC_gcollect();
        else if (useCollect)
            GC.Collect();

        dict->ForEach((_, x) => {
            callerCount++;
            Assert.IsTrue(x->Assert());
        });

        Assert.AreEqual(size, callerCount);
        Assert.AreEqual(size, dict->Count);
        watch.Stop();
        TestContext.Out.WriteLine($"Time elapsed: {watch.Elapsed:G}");
    }


    [TestCase(true, 10)]
    [TestCase(true, 100)]
    [TestCase(true, 1000)]
    [TestCase(true, 10000)]
    [TestCase(false, 10)]
    [TestCase(false, 100)]
    [TestCase(false, 1000)]
    [TestCase(false, 10000)]

    [TestCase(true, 10, true)]
    [TestCase(true, 100, true)]
    [TestCase(true, 1000, true)]
    [TestCase(true, 10000, true)]
    [TestCase(false, 10, true)]
    [TestCase(false, 100, true)]
    [TestCase(false, 1000, true)]
    [TestCase(false, 10000, true)]
    public void CreateAtomicDictionary(bool boehm, int size, bool useCollect = false)
    {
        var watch = Stopwatch.StartNew();
        var allocator = default(AllocatorBlock);

        if (boehm)
        {
            allocator = new AllocatorBlock
            {
                alloc = &IshtarGC_Alloc,
                alloc_primitives = &IshtarGC_AtomicAlloc,
                free = &IshtarGC_Free,
                realloc = &IshtarGC_Realloc
            };
        }
        else
        {
            allocator = new AllocatorBlock
            {
                alloc = &NativeMemory_AllocZeroed,
                alloc_primitives = &NativeMemory_AllocZeroed,
                free = &NativeMemory_Free,
                realloc = &NativeMemory_Realloc
            };
        }

        var dict = AtomicNativeDictionary<int, Magic>.Create(16, allocator);

        foreach (int i in Enumerable.Range(0, size)) dict->Add(i, Magic.CreateAtomic());


        var callerCount = 0;

        if (useCollect && boehm)
            BoehmGCLayout.Native.GC_gcollect();
        else if (useCollect)
            GC.Collect();

        dict->ForEach(((int key, ref Magic x) =>
        {
            callerCount++;
            Assert.IsTrue(x.Assert());
        }));

        Assert.AreEqual(size, callerCount);
        Assert.AreEqual(size, dict->Count);
        watch.Stop();
        TestContext.Out.WriteLine($"Time elapsed: {watch.Elapsed:G}");
    }



    [TestCase(true)]
    public void TestEq(bool boehm)
    {
        var allocator = default(AllocatorBlock);

        if (boehm)
        {
            allocator = new AllocatorBlock
            {
                alloc = &IshtarGC_Alloc,
                alloc_primitives = &IshtarGC_AtomicAlloc,
                free = &IshtarGC_Free,
                realloc = &IshtarGC_Realloc
            };
        }
        else
        {
            allocator = new AllocatorBlock
            {
                alloc = &NativeMemory_AllocZeroed,
                alloc_primitives = &NativeMemory_AllocZeroed,
                free = &NativeMemory_Free,
                realloc = &NativeMemory_Realloc
            };
        }


        var list = NativeList<Magic>.Create(16, allocator);

        var item1 = Magic.Create(12);
        var item2 = Magic.Create(14);
        var item3 = Magic.Create(16);
        list->Add(item1);
        list->Add(item2);
        list->Add(item3);

        var target = list->FirstOrNull(x => x->i1 == 16);

        Assert.AreEqual(target->i1, item3->i1);

        list->Remove(target);

        Assert.AreEqual(2, list->Count);

        target = list->FirstOrNull(x => x->i1 == 16);

        Assert.IsTrue(target is null);
    }

    public static void* NativeMemory_AllocZeroed(uint size)
        => NativeMemory.AllocZeroed(size);

    public static void* IshtarGC_Alloc(uint size)
        => BoehmGCLayout.Native.GC_malloc(size);
    public static void* IshtarGC_AtomicAlloc(uint size)
        => BoehmGCLayout.Native.GC_malloc_atomic(size);

    public static void NativeMemory_Free(void* ptr)
        => NativeMemory.Free(ptr);

    public static void IshtarGC_Free(void* ptr)
        => BoehmGCLayout.Native.GC_free(ptr);

    public static void* NativeMemory_Realloc(void* ptr, uint newBytes)
        => NativeMemory.Realloc(ptr, newBytes);

    public static void* IshtarGC_Realloc(void* ptr, uint newBytes)
        => (void*)BoehmGCLayout.Native.GC_realloc((nint)ptr, newBytes);

    public static bool Comparer(Magic* p1, Magic* p2)
    {
        //Assert.IsTrue(p1->Assert());
        //Assert.IsTrue(p2->Assert());
        return p1->i1 == p2->i1 && p1->i2 == p2->i2 && p1->i3 == p2->i3 && p1->i4 == p2->i4;
    }

    public static bool Comparer(ref Magic p1, ref Magic p2)
    {
        Assert.IsTrue(p1.Assert());
        Assert.IsTrue(p2.Assert());
        return p1.i1 == p2.i1 && p1.i2 == p2.i2 && p1.i3 == p2.i3 && p1.i4 == p2.i4;
    }
}


public unsafe struct Magic : IEquatable<Magic>, IEq<Magic>
{
    public int i1;
    public int i2;
    public float i3;
    public int i4;

    public static Magic* Create()
    {
        var p = (Magic*)NativeMemory.AllocZeroed((uint)sizeof(Magic));

        *p = new Magic();

        p->i1 = 1448;
        p->i2 = 228;
        p->i3 = 14.48f;
        p->i4 = 5252;

        return p;
    }

    public static Magic* Create(int i1)
    {
        var p = (Magic*)NativeMemory.AllocZeroed((uint)sizeof(Magic));

        *p = new Magic();

        p->i1 = i1;
        p->i2 = i1 * 228;
        p->i3 = i1 * 14.48f;
        p->i4 = i1 * 5252;

        return p;
    }

    public static Magic CreateAtomic()
    {
        var p = new Magic
        {
            i1 = 1448,
            i2 = 228,
            i3 = 14.48f,
            i4 = 5252
        };

        return p;
    }

    public bool Assert() => i1 == 1448 && i2 == 228 && i3 == 14.48f && i4 == 5252;

    public bool Equals(Magic other) => i1 == other.i1 && i2 == other.i2 && i3.Equals(other.i3) && i4 == other.i4;

    public static bool Eq(Magic* p1, Magic* p2) => Tests.Comparer(p1, p2);

    public override bool Equals(object? obj) => obj is Magic other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(i1, i2, i3, i4);
}
