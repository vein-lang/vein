namespace ishtar_test
{
    using ishtar;
    using vein.runtime;
    using NUnit.Framework;

    [TestFixture]
    public unsafe class ArrayTest : IshtarTestBase
    {
        [Test]
        [Parallelizable(ParallelScope.None)]
        public void NewArr()
        {
            using var ctx = CreateContext();
            var arrType = VeinTypeCode.TYPE_I4.AsClass()(Types).FullName;

            ctx.EnsureType(arrType);

            var result = ctx.Execute((x, _) =>
            {
                x.Emit(OpCodes.LD_TYPE, arrType);   /* load array elements type */
                x.Emit(OpCodes.LDC_I8_5);           /* load size                */
                x.Emit(OpCodes.NEWARR);             /* create array             */
                x.Emit(OpCodes.RET);                /* return instance of array */
            });


            Assert.AreEqual(VeinTypeCode.TYPE_ARRAY, result.returnValue->type);
        }

        [Test]
        [Parallelizable(ParallelScope.None)]
        public void LoadAndStageElementTest()
        {
            using var ctx = CreateContext();
            var arrType = VeinTypeCode.TYPE_I4.AsClass()(Types).FullName;

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

            Assert.AreEqual(VeinTypeCode.TYPE_I4, result.returnValue->type);
            Assert.AreEqual(3, result.returnValue->data.i);
        }

        [Test]
        [Parallelizable(ParallelScope.None)]
        public void GetLenTest()
        {
            using var ctx = CreateContext();
            var arrType = VeinTypeCode.TYPE_I4.AsClass()(Types).FullName;

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

            Assert.AreEqual(VeinTypeCode.TYPE_U8, result.returnValue->type);
            Assert.AreEqual(5, result.returnValue->data.i);
        }


        [Test]
        [Parallelizable(ParallelScope.None)]
        public void StringFormatWithArray()
        {
            using var ctx = CreateContext();

            var result = ctx.Execute((x, _) =>
            {
                x.Emit(OpCodes.LDC_STR, "{0} and {1}");
                x.Emit(OpCodes.LDC_STR, "foo");
                x.Emit(OpCodes.LDC_STR, "bar");
                x.Emit(OpCodes.CALL, (Types).StringClass.FindMethod("format", new []
                {
                    (Types).StringClass, /* template */ 
                    (Types).ObjectClass, /* o1 */
                    (Types).ObjectClass  /* o2 */
                }));
                x.Emit(OpCodes.RET);
            });

            Assert.AreEqual(VeinTypeCode.TYPE_STRING, result.returnValue->type);
            var str = IshtarMarshal.ToDotnetString((IshtarObject*)result.returnValue->data.p, result);
            Assert.AreEqual("foo and bar", str);
        }
    }
}
