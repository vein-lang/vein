namespace ishtar_test
{
    using System;
    using System.Linq;
    using ishtar;
    using vein.extensions;
    using vein.runtime;
    using NUnit.Framework;

    [TestFixture]
    public class GCTest : IshtarTestBase
    {
        [Test]
        [Parallelizable(ParallelScope.None)]
        public unsafe void CorrectAllocateInt()
        {
            var result = GC.ToIshtarObject(1, GetVM().Frames.EntryPoint);
            GC.FreeObject(&result, GetVM().Frames.EntryPoint);
        }

        [Test]
        [Parallelizable(ParallelScope.None)]
        public unsafe void CorrectAllocateObject()
        {
            var result = GC.AllocObject(VeinTypeCode.TYPE_I8.AsRuntimeClass(Types), GetVM().Frames.EntryPoint);
            GC.FreeObject(&result, GetVM().Frames.EntryPoint);
        }

        [Test]
        [Parallelizable(ParallelScope.None)]
        public unsafe void CorrectAllocateString()
        {
            var result = GC.ToIshtarObject("foo", GetVM().Frames.EntryPoint);
            GC.FreeObject(&result, GetVM().Frames.EntryPoint);
        }

        [Test]
        [Parallelizable(ParallelScope.None)]
        public unsafe void CorrectAllocateValue()
        {
            var result = GC.AllocValue(GetVM().Frames.EntryPoint);
            result = null;
        }

        [Test]
        [Parallelizable(ParallelScope.None)]
        public unsafe void PopulateObjectsAndShutdownVM()
        {
            GC.check_memory_leak = false;

            foreach (int i in Enumerable.Range(0, 5))
            {
                GC.AllocObject(VeinTypeCode.TYPE_I8.AsRuntimeClass(Types), GetVM().Frames.EntryPoint);
            }

            this.GC.VM.Dispose();


            Assert.AreEqual("1 objects", $"{GC.Stats.alive_objects} objects");
            Assert.AreEqual("24 bytes", $"{GC.Stats.total_bytes_requested} bytes");
        }

        [Test]
        [Parallelizable(ParallelScope.None)]
        public unsafe void AllocateAndFreeObject()
        {
            var obj = GC.AllocObject(VeinTypeCode.TYPE_I8.AsRuntimeClass(Types), GetVM().Frames.EntryPoint);
            GC.FreeObject(obj, this.GetVM().Frames.EntryPoint);
        }

        [Test]
        [Parallelizable(ParallelScope.None)]
        public unsafe void CorrectAllocateArray()
        {
            var array = GC.AllocArray(VeinTypeCode.TYPE_I4.AsRuntimeClass(Types), 10, 1);

            Assert.AreEqual(10UL, array->length);
            Assert.AreEqual(1UL, array->rank);

            foreach (var i in ..10)
                array->Set((uint)i, GC.ToIshtarObject(88 * i, GetVM().Frames.EntryPoint), GetVM().Frames.EntryPoint);

            foreach (var i in ..10)
            {
                var obj = array->Get((uint) i, GetVM().Frames.EntryPoint);
                Assert.AreEqual(i * 88, IshtarMarshal.ToDotnetInt32(obj, null));
            }
        }
        protected override void StartUp() { }
        protected override void Shutdown() { }
    }
}
