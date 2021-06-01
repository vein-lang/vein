namespace ishtar_test
{
    using ishtar;
    using mana.ishtar.emit;
    using mana.runtime;
    using Xunit;

    public unsafe class InstructionSizeTest : IshtarTestBase
    {
        [Fact]
        public void I8FillTest()
        {
            using var ctx = CreateContext();

            var result = ctx.Execute((gen, _) =>
            {
                gen.Emit(OpCodes.LDC_I8_S, long.MaxValue);
                gen.Emit(OpCodes.RET);
            });

            Assert.Equal(ManaTypeCode.TYPE_I8, (*result.returnValue).type);
            Assert.Equal(long.MaxValue, (*result.returnValue).data.l);
        }
    }
}