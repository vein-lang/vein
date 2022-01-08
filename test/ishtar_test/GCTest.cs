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
            var result = IshtarMarshal.ToIshtarObject(1);
            IshtarGC.FreeObject(&result, new CallFrame());
        }

        [Test]
        [Parallelizable(ParallelScope.None)]
        public unsafe void CorrectAllocateObject()
        {
            var result = IshtarGC.AllocObject(VeinTypeCode.TYPE_I8.AsRuntimeClass());
            IshtarGC.FreeObject(&result, new CallFrame());
        }

        [Test]
        [Parallelizable(ParallelScope.None)]
        public unsafe void CorrectAllocateString()
        {
            var result = IshtarMarshal.ToIshtarObject("foo");
            IshtarGC.FreeObject(&result, new CallFrame());
        }

        [Test]
        [Parallelizable(ParallelScope.None)]
        public unsafe void CorrectAllocateValue()
        {
            var result = IshtarGC.AllocValue();
            result = null;
        }

        [Test]
        [Parallelizable(ParallelScope.None)]
        public unsafe void CorrectAllocateArray()
        {
            if (VM.watcher is DefaultWatchDog)
                VM.watcher = new TestWatchDog();

            var array = IshtarGC.AllocArray(VeinTypeCode.TYPE_I4.AsRuntimeClass(), 10, 1);

            Assert.AreEqual(10UL, array->length);
            Assert.AreEqual(1UL, array->rank);

            foreach (var i in ..10)
                array->Set((uint)i, IshtarMarshal.ToIshtarObject(88 * i));

            foreach (var i in ..10)
            {
                var obj = array->Get((uint) i);
                Assert.AreEqual(i * 88, IshtarMarshal.ToDotnetInt32(obj, null));
            }
        }
        protected override void StartUp() { }
        protected override void Shutdown() { }
    }
}
