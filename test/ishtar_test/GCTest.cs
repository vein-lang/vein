namespace ishtar_test
{
    using ishtar;
    using mana.runtime;
    using Xunit;

    public class GCTest : IshtarTestBase
    {
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
            var array = IshtarGC.AllocArray(ManaTypeCode.TYPE_I4.AsRuntimeClass(), 10, 1);

            Assert.Equal(10UL, array->length);
            Assert.Equal(1UL, array->rank);
        }
        protected override void StartUp() { }
        protected override void Shutdown() { }
    }
}
