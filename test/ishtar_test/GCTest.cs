namespace ishtar_test
{
    using System;
    using ishtar;
    using mana.extensions;
    using mana.runtime;
    using Xunit;
    using Xunit.Abstractions;

    public class GCTest : IshtarTestBase
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public GCTest(ITestOutputHelper testOutputHelper)
            => _testOutputHelper = testOutputHelper;

        [Fact]
        public unsafe void CorrectAllocateInt()
        {
            var result = IshtarMarshal.ToIshtarObject(1);
            IshtarGC.FreeObject(&result);
        }

        [Fact]
        public unsafe void CorrectAllocateObject()
        {
            var result = IshtarGC.AllocObject(ManaTypeCode.TYPE_I8.AsRuntimeClass());
            IshtarGC.FreeObject(&result);
        }

        [Fact]
        public unsafe void CorrectAllocateString()
        {
            var result = IshtarMarshal.ToIshtarObject("foo");
            IshtarGC.FreeObject(&result);
        }

        [Fact]
        public unsafe void CorrectAllocateValue()
        {
            var result = IshtarGC.AllocValue();
            result = null;
        }

        [Fact]
        public unsafe void CorrectAllocateArray()
        {
            if (VM.watcher is DefaultWatchDog)
                VM.watcher = new TestWatchDog();

            var array = IshtarGC.AllocArray(ManaTypeCode.TYPE_I4.AsRuntimeClass(), 10, 1);

            Assert.Equal(10UL, array->length);
            Assert.Equal(1UL, array->rank);

            foreach (var i in ..10)
                array->Set((uint)i, IshtarMarshal.ToIshtarObject(88 * i));

            foreach (var i in ..10)
            {
                var obj = array->Get((uint) i);
                Assert.Equal(i * 88, IshtarMarshal.ToDotnetInt32(obj, null));
            }
        }
        protected override void StartUp() { }
        protected override void Shutdown() { }
    }
}
