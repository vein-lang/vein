namespace ishtar_test
{
    using System;
    using ishtar;
    using mana.ishtar.emit;
    using mana.runtime;
    using NUnit.Framework;
    using static mana.runtime.FieldFlags;

    [TestFixture]
    public class InstructionTest : IshtarTestBase
    {
        [Test]
        [TestCase(OpCodeValue.LDC_I8_5, ManaTypeCode.TYPE_I8)]
        [TestCase(OpCodeValue.LDC_I4_5, ManaTypeCode.TYPE_I4)]
        [TestCase(OpCodeValue.LDC_I2_5, ManaTypeCode.TYPE_I2)]
        [Parallelizable(ParallelScope.None)]
        public unsafe void ADD_Test(OpCodeValue op, ManaTypeCode code)
        {
            using var ctx = CreateContext();
            var result = ctx.Execute((gen, _) =>
            {
                gen.Emit(OpCodes.all[op]);
                gen.Emit(OpCodes.all[op]);
                gen.Emit(OpCodes.ADD);
                gen.Emit(OpCodes.RET);
            });
            Validate();
            Assert.AreEqual(code, (*result.returnValue).type);
            Assert.AreEqual(10, (*result.returnValue).data.l);
        }

        [Test]
        [TestCase(OpCodeValue.LDC_I8_5, ManaTypeCode.TYPE_I8)]
        [TestCase(OpCodeValue.LDC_I4_5, ManaTypeCode.TYPE_I4)]
        [TestCase(OpCodeValue.LDC_I2_5, ManaTypeCode.TYPE_I2)]
        [Parallelizable(ParallelScope.None)]
        public unsafe void SUB_Test(OpCodeValue op, ManaTypeCode code)
        {
            using var ctx = CreateContext();
            var result = ctx.Execute((gen, _) =>
            {
                gen.Emit(OpCodes.all[op]);
                gen.Emit(OpCodes.all[op]);
                gen.Emit(OpCodes.all[op]);
                gen.Emit(OpCodes.SUB);
                gen.Emit(OpCodes.SUB);
                gen.Emit(OpCodes.RET);
            });
            Validate();
            Assert.AreEqual(code, (*result.returnValue).type);
            Assert.AreEqual(5, (*result.returnValue).data.l);
        }

        [Test]
        [TestCase(OpCodeValue.LDC_I8_5, ManaTypeCode.TYPE_I8)]
        [TestCase(OpCodeValue.LDC_I4_5, ManaTypeCode.TYPE_I4)]
        [TestCase(OpCodeValue.LDC_I2_5, ManaTypeCode.TYPE_I2)]
        [Parallelizable(ParallelScope.None)]
        public unsafe void MUL_Test(OpCodeValue op, ManaTypeCode code)
        {
            using var ctx = CreateContext();
            var result = ctx.Execute((gen, _) =>
            {
                gen.Emit(OpCodes.all[op]);
                gen.Emit(OpCodes.all[op]);
                gen.Emit(OpCodes.MUL);
                gen.Emit(OpCodes.RET);
            });
            Validate();
            Assert.AreEqual(code, (*result.returnValue).type);
            Assert.AreEqual(5 * 5, (*result.returnValue).data.l);
        }

        [Test]
        [TestCase(OpCodeValue.LDC_I8_5, ManaTypeCode.TYPE_I8)]
        [TestCase(OpCodeValue.LDC_I4_5, ManaTypeCode.TYPE_I4)]
        [TestCase(OpCodeValue.LDC_I2_5, ManaTypeCode.TYPE_I2)]
        [Parallelizable(ParallelScope.None)]
        public unsafe void DIV_Test(OpCodeValue op, ManaTypeCode code)
        {
            using var ctx = CreateContext();
            var result = ctx.Execute((gen, _) =>
            {
                gen.Emit(OpCodes.all[op]);
                gen.Emit(OpCodes.all[op]);
                gen.Emit(OpCodes.DIV);
                gen.Emit(OpCodes.RET);
            });
            Validate();
            Assert.AreEqual(code, (*result.returnValue).type);
            Assert.AreEqual(5 / 5, (*result.returnValue).data.l);
        }

        [Test]
        [TestCase(6f, OpCodeValue.DIV)]
        [TestCase(24f, OpCodeValue.MUL)]
        [TestCase(10f, OpCodeValue.SUB)]
        [TestCase(14f, OpCodeValue.ADD)]
        [Parallelizable(ParallelScope.None)]
        public unsafe void Arithmetic_Float_Test(float expected, OpCodeValue actor)
        {
            using var ctx = CreateContext();
            var result = ctx.Execute((gen, _) =>
            {
                gen.Emit(OpCodes.LDC_F4, 12f);
                gen.Emit(OpCodes.LDC_F4, 2f);
                gen.Emit(OpCodes.all[actor]);
                gen.Emit(OpCodes.RET);
            });
            Validate();
            Assert.AreEqual(ManaTypeCode.TYPE_R4, (*result.returnValue).type);
            Assert.AreEqual(expected, (*result.returnValue).data.f_r4);
        }

        [Test]
        [TestCase(6, OpCodeValue.DIV)]
        [TestCase(24, OpCodeValue.MUL)]
        [TestCase(10, OpCodeValue.SUB)]
        [TestCase(14, OpCodeValue.ADD)]
        [Parallelizable(ParallelScope.None)]
        public unsafe void Arithmetic_Decimal_Test(int expected, OpCodeValue actor)
        {
            using var ctx = CreateContext();
            var result = ctx.Execute((gen, _) =>
            {
                gen.Emit(OpCodes.LDC_F16, 12m);
                gen.Emit(OpCodes.LDC_F16, 2m);
                gen.Emit(OpCodes.all[actor]);
                gen.Emit(OpCodes.RET);
            });
            Validate();
            Assert.AreEqual(ManaTypeCode.TYPE_R16, (*result.returnValue).type);
            Assert.AreEqual((decimal)expected, (*result.returnValue).data.d);
        }

        [Test]
        [TestCase(6, OpCodeValue.DIV)]
        [TestCase(24, OpCodeValue.MUL)]
        [TestCase(10, OpCodeValue.SUB)]
        [TestCase(14, OpCodeValue.ADD)]
        [Parallelizable(ParallelScope.None)]
        public unsafe void Arithmetic_Double_Test(int expected, OpCodeValue actor)
        {
            using var ctx = CreateContext();
            var result = ctx.Execute((gen, _) =>
            {
                gen.Emit(OpCodes.LDC_F8, 12d);
                gen.Emit(OpCodes.LDC_F8, 2d);
                gen.Emit(OpCodes.all[actor]);
                gen.Emit(OpCodes.RET);
            });
            Validate();
            Assert.AreEqual(ManaTypeCode.TYPE_R8, (*result.returnValue).type);
            Assert.AreEqual((double)expected, (*result.returnValue).data.f);
        }

        [Test]
        [TestCase(OpCodeValue.LDC_I8_5, ManaTypeCode.TYPE_I8)]
        [TestCase(OpCodeValue.LDC_I4_5, ManaTypeCode.TYPE_I4)]
        [TestCase(OpCodeValue.LDC_I2_5, ManaTypeCode.TYPE_I2)]
        [Parallelizable(ParallelScope.None)]
        public unsafe void DUP_Test(OpCodeValue op, ManaTypeCode code)
        {
            using var ctx = CreateContext();
            var result = ctx.Execute((gen, _) =>
            {
                gen.Emit(OpCodes.all[op]);
                gen.Emit(OpCodes.DUP);
                gen.Emit(OpCodes.MUL);
                gen.Emit(OpCodes.RET);
            });
            Validate();
            Assert.AreEqual(code, (*result.returnValue).type);
            Assert.AreEqual(5 * 5, (*result.returnValue).data.l);
        }

        [Test]
        [Parallelizable(ParallelScope.None)]
        public unsafe void LDSTR_Test()
        {
            var targetStr = "the string";
            using var ctx = CreateContext();
            var result = ctx.Execute((gen, _) =>
            {
                gen.Emit(OpCodes.LDC_STR, targetStr);
                gen.Emit(OpCodes.RET);
            });
            Validate();
            Assert.AreEqual(ManaTypeCode.TYPE_STRING, (*result.returnValue).type);

            var obj = (IshtarObject*) result.returnValue->data.p;
            Assert.AreEqual(targetStr, IshtarMarshal.ToDotnetString(obj, null));
        }


        [Test]
        [Parallelizable(ParallelScope.None)]
        public unsafe void LDF_Test()
        {
            using var ctx = CreateContext();

            ctx.OnClassBuild((@class, o) =>
            {
                o.field = @class.DefineField("TEST_FIELD", Public | Static, ManaTypeCode.TYPE_I4.AsClass());
            });


            var result = ctx.Execute((gen, storage) =>
            {
                gen.Emit(OpCodes.LDC_I4_S, 142);
                gen.Emit(OpCodes.STSF, storage.field);
                gen.Emit(OpCodes.LDSF, storage.field);
                gen.Emit(OpCodes.RET);
            });

            Assert.AreEqual(142, result.returnValue->data.l);
        }


        protected override void StartUp()
        {

        }

        protected override void Shutdown()
        {
        }
    }
}
