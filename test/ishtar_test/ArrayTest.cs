namespace ishtar_test
{
    using ishtar;
    using mana.runtime;
    using Xunit;

    public unsafe class ArrayTest : IshtarTestBase
    {
        [Fact]
        public void NewArr()
        {
            using var ctx = CreateContext();
            var arrType = ManaTypeCode.TYPE_I4.AsClass().FullName;

            ctx.EnsureType(arrType);

            var result = ctx.Execute((x, _) =>
            {
                x.Emit(OpCodes.LD_TYPE, arrType);   /* load array elements type */
                x.Emit(OpCodes.LDC_I8_5);           /* load size                */
                x.Emit(OpCodes.NEWARR);             /* create array             */
                x.Emit(OpCodes.RET);                /* return instance of array */
            });


            Assert.Equal(ManaTypeCode.TYPE_ARRAY, result.returnValue->type);
        }

        [Fact]
        public void LoadAndStageElementTest()
        {
            using var ctx = CreateContext();
            var arrType = ManaTypeCode.TYPE_I4.AsClass().FullName;

            ctx.EnsureType(arrType);

            var result = ctx.Execute((x, _) =>
            {
                x.Emit(OpCodes.LD_TYPE, arrType);
                x.Emit(OpCodes.LDC_I8_5);
                x.Emit(OpCodes.NEWARR);
                x.Emit(OpCodes.LDC_I4_2);
                x.Emit(OpCodes.STELEM_S, 0);
                x.Emit(OpCodes.LDC_I4_3);
                x.Emit(OpCodes.STELEM_S, 1);
                x.Emit(OpCodes.LDELEM_S, 1);
                x.Emit(OpCodes.RET);
            });

            Assert.Equal(ManaTypeCode.TYPE_I4, result.returnValue->type);
            Assert.Equal(3, result.returnValue->data.i);
        }

        [Fact]
        public void GetLenTest()
        {
            using var ctx = CreateContext();
            var arrType = ManaTypeCode.TYPE_I4.AsClass().FullName;

            ctx.EnsureType(arrType);

            var result = ctx.Execute((x, _) =>
            {
                x.Emit(OpCodes.LD_TYPE, arrType);
                x.Emit(OpCodes.LDC_I8_5);
                x.Emit(OpCodes.NEWARR);
                x.Emit(OpCodes.LDC_I4_2);
                x.Emit(OpCodes.STELEM_S, 0);
                x.Emit(OpCodes.LDC_I4_3);
                x.Emit(OpCodes.STELEM_S, 1);
                x.Emit(OpCodes.LDLEN);
                x.Emit(OpCodes.RET);
            });

            Assert.Equal(ManaTypeCode.TYPE_U8, result.returnValue->type);
            Assert.Equal(5, result.returnValue->data.i);
        }
    }
}
