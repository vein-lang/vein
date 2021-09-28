namespace ishtar_test
{
    using ishtar;
    using mana.ishtar.emit;
    using mana.runtime;
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

            Assert.AreEqual(ManaTypeCode.TYPE_I8, (*result.returnValue).type);
            Assert.AreEqual(long.MaxValue, (*result.returnValue).data.l);
        }
    }
}
