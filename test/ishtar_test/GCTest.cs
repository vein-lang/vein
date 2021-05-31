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
        protected override void StartUp() { }
        protected override void Shutdown() { }
    }
}