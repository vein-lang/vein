namespace ishtar_test
{
    using System;
    using System.Globalization;
    using ishtar;
    using vein.runtime;
    using NUnit.Framework;
    using static vein.runtime.FieldFlags;
    using Org.BouncyCastle.Asn1.X509;
    using System.Collections.Generic;
    using System.Linq;
    using Org.BouncyCastle.Asn1.Cmp;
    using Org.BouncyCastle.Crypto.Prng;

    [TestFixture]
    public class AInstructionTestBase : IshtarTestBase
    {
        [Test]
        [TestCase(OpCodeValue.LDC_I8_5, VeinTypeCode.TYPE_I8)]
        [TestCase(OpCodeValue.LDC_I4_5, VeinTypeCode.TYPE_I4)]
        [TestCase(OpCodeValue.LDC_I2_5, VeinTypeCode.TYPE_I2)]
        [Parallelizable(ParallelScope.None)]
        public unsafe void ADD_Test(OpCodeValue op, VeinTypeCode code)
        {
            using var scope = CreateScope();

            scope.OnCodeBuild((gen, _) =>
            {
                gen.Emit(OpCodes.all[op]);
                gen.Emit(OpCodes.all[op]);
                gen.Emit(OpCodes.ADD);
                gen.Emit(OpCodes.RET);
            });

            var result = scope
                .Compile()
                .Execute()
                .Validate();

            Assert.AreEqual(code, (result->returnValue[0]).type);
            Assert.AreEqual(10, (result->returnValue[0]).data.l);
        }

        [Test]
        [TestCase(OpCodeValue.LDC_I8_5, VeinTypeCode.TYPE_I8)]
        [TestCase(OpCodeValue.LDC_I4_5, VeinTypeCode.TYPE_I4)]
        [TestCase(OpCodeValue.LDC_I2_5, VeinTypeCode.TYPE_I2)]
        [Parallelizable(ParallelScope.None)]
        public unsafe void SUB_Test(OpCodeValue op, VeinTypeCode code)
        {
            using var scope = CreateScope();

            scope.OnCodeBuild((gen, _) => {
                gen.Emit(OpCodes.all[op]);
                gen.Emit(OpCodes.all[op]);
                gen.Emit(OpCodes.all[op]);
                gen.Emit(OpCodes.SUB);
                gen.Emit(OpCodes.SUB);
                gen.Emit(OpCodes.RET);
            });

            var result = scope
                .Compile()
                .Execute()
                .Validate();

            Assert.AreEqual(code, (result->returnValue[0]).type);
            Assert.AreEqual(5, (result->returnValue[0]).data.l);
        }

        [Test]
        [TestCase(OpCodeValue.LDC_I8_5, VeinTypeCode.TYPE_I8)]
        [TestCase(OpCodeValue.LDC_I4_5, VeinTypeCode.TYPE_I4)]
        [TestCase(OpCodeValue.LDC_I2_5, VeinTypeCode.TYPE_I2)]
        [Parallelizable(ParallelScope.None)]
        public unsafe void MUL_Test(OpCodeValue op, VeinTypeCode code)
        {
            using var scope = CreateScope();

            scope.OnCodeBuild((gen, _) => {
                gen.Emit(OpCodes.all[op]);
                gen.Emit(OpCodes.all[op]);
                gen.Emit(OpCodes.MUL);
                gen.Emit(OpCodes.RET);
            });

            var result = scope
                .Compile()
                .Execute()
                .Validate();

            Assert.AreEqual(code, (result->returnValue[0]).type);
            Assert.AreEqual(5 * 5, (result->returnValue[0]).data.l);
        }

        [Test]
        [TestCase(OpCodeValue.LDC_I8_5, VeinTypeCode.TYPE_I8)]
        [TestCase(OpCodeValue.LDC_I4_5, VeinTypeCode.TYPE_I4)]
        [TestCase(OpCodeValue.LDC_I2_5, VeinTypeCode.TYPE_I2)]
        [Parallelizable(ParallelScope.None)]
        public unsafe void DIV_Test(OpCodeValue op, VeinTypeCode code)
        {
            using var scope = CreateScope();

            scope.OnCodeBuild((gen, _) => {
                gen.Emit(OpCodes.all[op]);
                gen.Emit(OpCodes.all[op]);
                gen.Emit(OpCodes.DIV);
                gen.Emit(OpCodes.RET);
            });

            var result = scope
                .Compile()
                .Execute()
                .Validate();

            Assert.AreEqual(code, (result->returnValue[0]).type);
            Assert.AreEqual(5 / 5, (result->returnValue[0]).data.l);
        }

        [Test]
        [TestCase(OpCodeValue.LDC_I8_5, VeinTypeCode.TYPE_I8)]
        [TestCase(OpCodeValue.LDC_I4_5, VeinTypeCode.TYPE_I4)]
        [TestCase(OpCodeValue.LDC_I2_5, VeinTypeCode.TYPE_I2)]
        [Parallelizable(ParallelScope.None)]
        public unsafe void MOD_Test(OpCodeValue op, VeinTypeCode code)
        {
            using var scope = CreateScope();

            scope.OnCodeBuild((gen, _) => {
                gen.Emit(OpCodes.all[op]);
                gen.Emit(OpCodes.all[op]);
                gen.Emit(OpCodes.MOD);
                gen.Emit(OpCodes.RET);
            });

            var result = scope
                .Compile()
                .Execute()
                .Validate();

            Assert.AreEqual(code, (result->returnValue[0]).type);
            Assert.AreEqual(5 % 5, (result->returnValue[0]).data.l);
        }

        [Test]
        [TestCase(6f, OpCodeValue.DIV)]
        [TestCase(24f, OpCodeValue.MUL)]
        [TestCase(10f, OpCodeValue.SUB)]
        [TestCase(14f, OpCodeValue.ADD)]
        [Parallelizable(ParallelScope.None)]
        public unsafe void Arithmetic_Float_Test(float expected, OpCodeValue actor)
        {
            using var scope = CreateScope();

            scope.OnCodeBuild((gen, _) => {
                gen.Emit(OpCodes.LDC_F4, 12f);
                gen.Emit(OpCodes.LDC_F4, 2f);
                gen.Emit(OpCodes.all[actor]);
                gen.Emit(OpCodes.RET);
            });

            var result = scope
                .Compile()
                .Execute()
                .Validate();

            Assert.AreEqual(VeinTypeCode.TYPE_R4, (result->returnValue[0]).type);
            Assert.AreEqual(expected, (result->returnValue[0]).data.f_r4);
        }

        [Test, Ignore("Emit 'ldc.i2.s' resulted in an invalid buffer size value. amount: 4, excepted: 2")]
        [TestCase(6, OpCodeValue.DIV)]
        [TestCase(24, OpCodeValue.MUL)]
        [TestCase(10, OpCodeValue.SUB)]
        [TestCase(14, OpCodeValue.ADD)]
        [Parallelizable(ParallelScope.None)]
        public unsafe void Arithmetic_LDC_I2_Test(float expected, OpCodeValue actor)
        {
            using var scope = CreateScope();

            scope.OnCodeBuild((gen, _) => {
                gen.Emit(OpCodes.LDC_I2_S, 12);
                gen.Emit(OpCodes.LDC_I2_S, 2);
                gen.Emit(OpCodes.all[actor]);
                gen.Emit(OpCodes.RET);
            });

            var result = scope
                .Compile()
                .Execute()
                .Validate();

            Assert.AreEqual(VeinTypeCode.TYPE_R4, (result->returnValue[0]).type);
            Assert.AreEqual(expected, (result->returnValue[0]).data.f_r4);
        }

        [Test]
        [TestCase(6, OpCodeValue.DIV)]
        [TestCase(24, OpCodeValue.MUL)]
        [TestCase(10, OpCodeValue.SUB)]
        [TestCase(14, OpCodeValue.ADD)]
        [Parallelizable(ParallelScope.None)]
        public unsafe void Arithmetic_Decimal_Test(int expected, OpCodeValue actor)
        {
            using var scope = CreateScope();

            scope.OnCodeBuild((gen, _) => {
                gen.Emit(OpCodes.LDC_F16, 12m);
                gen.Emit(OpCodes.LDC_F16, 2m);
                gen.Emit(OpCodes.all[actor]);
                gen.Emit(OpCodes.RET);
            });

            var result = scope
                .Compile()
                .Execute()
                .Validate();

            Assert.AreEqual(VeinTypeCode.TYPE_R16, (result->returnValue[0]).type);
            Assert.AreEqual((decimal)expected, (result->returnValue[0]).data.d);
        }

        [Test]
        [TestCase(6, OpCodeValue.DIV)]
        [TestCase(24, OpCodeValue.MUL)]
        [TestCase(10, OpCodeValue.SUB)]
        [TestCase(14, OpCodeValue.ADD)]
        [TestCase(0, OpCodeValue.MOD)]
        [Parallelizable(ParallelScope.None)]
        public unsafe void Arithmetic_Double_Test(int expected, OpCodeValue actor)
        {
            using var scope = CreateScope();

            scope.OnCodeBuild((gen, _) => {
                gen.Emit(OpCodes.LDC_F8, 12d);
                gen.Emit(OpCodes.LDC_F8, 2d);
                gen.Emit(OpCodes.all[actor]);
                gen.Emit(OpCodes.RET);
            });

            var result = scope
                .Compile()
                .Execute()
                .Validate();

            Assert.AreEqual(VeinTypeCode.TYPE_R8, (result->returnValue[0]).type);
            Assert.AreEqual((double)expected, (result->returnValue[0]).data.f);
        }

        [Test]
        [TestCaseSource(nameof(ArithmeticTestSource_Gen_Double))]
        [Parallelizable(ParallelScope.None)]
        public unsafe void Arithmetic_Gen_Double_Test(int s1, double s2, double expected, OpCodeValue actor)
        {
            using var scope = CreateScope();

            scope.OnCodeBuild((gen, _) => {
                gen.Emit(OpCodes.LDC_F8, (double)s1);
                gen.Emit(OpCodes.LDC_F8, s2);
                gen.Emit(OpCodes.all[actor]);
                gen.Emit(OpCodes.RET);
            });

            var result = scope
                .Compile()
                .Execute()
                .Validate();

            Assert.AreEqual(VeinTypeCode.TYPE_R8, (result->returnValue[0]).type);
            Assert.AreEqual(expected, (result->returnValue[0]).data.f);
        }


        static IEnumerable<object> ArithmeticTestSource_Gen_Double()
        {
            var rnd = new Random();
            foreach (var opcode in (OpCodeValue[])[OpCodeValue.ADD, OpCodeValue.SUB, OpCodeValue.DIV, OpCodeValue.MUL, OpCodeValue.MOD])
            {
                foreach (var i in Enumerable.Range(0, 30))
                {
                    var s1 = rnd.Next();
                    var s2 = rnd.NextDouble();
                    
                    object opt()
                    {
                        if (opcode == OpCodeValue.ADD)
                            return s1 + s2;
                        if (opcode == OpCodeValue.SUB)
                            return s1 - s2;
                        if (opcode == OpCodeValue.MUL)
                            return s1 * s2;
                        if (opcode == OpCodeValue.DIV)
                            return s1 / s2;
                        if (opcode == OpCodeValue.MOD)
                            return s1 % s2;
                        throw null;
                    }

                    yield return new[] { s1, s2, opt(), opcode };
                }
            }
        }


        [Test]
        [TestCase(OpCodeValue.LDC_I8_5, VeinTypeCode.TYPE_I8)]
        [TestCase(OpCodeValue.LDC_I4_5, VeinTypeCode.TYPE_I4)]
        [TestCase(OpCodeValue.LDC_I2_5, VeinTypeCode.TYPE_I2)]
        [Parallelizable(ParallelScope.None)]
        public unsafe void DUP_Test(OpCodeValue op, VeinTypeCode code)
        {
            using var scope = CreateScope();

            scope.OnCodeBuild((gen, _) => {
                gen.Emit(OpCodes.all[op]);
                gen.Emit(OpCodes.DUP);
                gen.Emit(OpCodes.MUL);
                gen.Emit(OpCodes.RET);
            });

            var result = scope
                .Compile()
                .Execute()
                .Validate();

            Assert.AreEqual(code, (result->returnValue[0]).type);
            Assert.AreEqual(5 * 5, (result->returnValue[0]).data.l);
        }

        [Test]
        [Parallelizable(ParallelScope.None)]
        public unsafe void LDSTR_Test()
        {
            var targetStr = "the string";

            using var scope = CreateScope();

            scope.OnCodeBuild((gen, _) => {
                gen.Emit(OpCodes.LDC_STR, targetStr);
                gen.Emit(OpCodes.RET);
            });

            var result = scope
                .Compile()
                .Execute()
                .Validate();

            Assert.AreEqual(VeinTypeCode.TYPE_STRING, (result->returnValue[0]).type);

            var obj = (IshtarObject*) result->returnValue[0].data.p;
            Assert.AreEqual(targetStr, IshtarMarshal.ToDotnetString(obj, null));
        }


        [Test]
        [Parallelizable(ParallelScope.None)]
        public unsafe void LDF_Test()
        {
            using var scope = CreateScope();

            scope.OnClassBuild((@class, o) => o.field = @class.DefineField("TEST_FIELD", Public | Static, VeinTypeCode.TYPE_I4.AsClass()(scope.Types)));



            scope.OnCodeBuild((gen, storage) => {
                gen.Emit(OpCodes.LDC_I4_S, 142);
                gen.Emit(OpCodes.STSF, (VeinField)storage.field);
                gen.Emit(OpCodes.LDSF, (VeinField)storage.field);
                gen.Emit(OpCodes.RET);
            });

            var result = scope
                .Compile()
                .Execute()
                .Validate();


            Assert.AreEqual(142, result->returnValue[0].data.l);
        }
    }
}
