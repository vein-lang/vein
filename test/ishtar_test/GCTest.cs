namespace ishtar_test
{
    using ishtar;
    using mana.runtime;
    using Xunit;

    public class GCTest : IshtarContext
    {
        [Fact]
        public unsafe void CorrectAllocateInt()
        {
            var result = IshtarGC.AllocInt(12);
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
            var result = IshtarGC.AllocString("test");
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