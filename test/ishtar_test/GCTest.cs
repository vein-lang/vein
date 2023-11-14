namespace ishtar_test
{
    using System;
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
            var result = GC.ToIshtarObject(1);
            GC.FreeObject(&result, GetVM().Frames.EntryPoint);
        }

        [Test]
        [Parallelizable(ParallelScope.None)]
        public unsafe void CorrectAllocateObject()
        {
            var result = GC.AllocObject(VeinTypeCode.TYPE_I8.AsRuntimeClass(Types));
            GC.FreeObject(&result, GetVM().Frames.EntryPoint);
        }

        [Test]
        [Parallelizable(ParallelScope.None)]
        public unsafe void CorrectAllocateString()
        {
            var result = GC.ToIshtarObject("foo");
            GC.FreeObject(&result, GetVM().Frames.EntryPoint);
        }

        [Test]
        [Parallelizable(ParallelScope.None)]
        public unsafe void CorrectAllocateValue()
        {
            var result = GC.AllocValue();
            result = null;
        }

        [Test]
        [Parallelizable(ParallelScope.None)]
        public unsafe void CorrectAllocateArray()
        {
            var array = GC.AllocArray(VeinTypeCode.TYPE_I4.AsRuntimeClass(Types), 10, 1);

            Assert.AreEqual(10UL, array->length);
            Assert.AreEqual(1UL, array->rank);

            foreach (var i in ..10)
                array->Set((uint)i, GC.ToIshtarObject(88 * i), GetVM().Frames.EntryPoint);

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
