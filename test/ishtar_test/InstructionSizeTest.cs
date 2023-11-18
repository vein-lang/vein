namespace ishtar_test
{
    using ishtar;
    using ishtar.emit;
    using vein.runtime;
    using NUnit.Framework;

    [TestFixture]
    public unsafe class InstructionSizeTest : IshtarTestBase
    {
        [Test]
        [Parallelizable(ParallelScope.None)]
        public void I8FillTest()
        {
            using var ctx = CreateContext();

            var result = ctx.Execute((gen, _) =>
            {
                gen.Emit(OpCodes.LDC_I8_S, long.MaxValue);
                gen.Emit(OpCodes.RET);
            });

            Assert.AreEqual(VeinTypeCode.TYPE_I8, (result.returnValue[0]).type);
            Assert.AreEqual(long.MaxValue, (result.returnValue[0]).data.l);
        }
    }
}
