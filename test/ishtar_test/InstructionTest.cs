namespace ishtar_test;

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
using Sprache;

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

        Equals(code, (result->returnValue[0]).type);
        Equals(10, (result->returnValue[0]).data.l);
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

        Equals(code, (result->returnValue[0]).type);
        Equals(5, (result->returnValue[0]).data.l);
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

        Equals(code, (result->returnValue[0]).type);
        Equals(5 * 5, (result->returnValue[0]).data.l);
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

        Equals(code, (result->returnValue[0]).type);
        Equals(5 / 5, (result->returnValue[0]).data.l);
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

        Equals(code, (result->returnValue[0]).type);
        Equals(5 % 5, (result->returnValue[0]).data.l);
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

        Equals(VeinTypeCode.TYPE_R4, (result->returnValue[0]).type);
        Equals(expected, (result->returnValue[0]).data.f_r4);
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

        Equals(VeinTypeCode.TYPE_R4, (result->returnValue[0]).type);
        Equals(expected, (result->returnValue[0]).data.f_r4);
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

        Equals(VeinTypeCode.TYPE_R16, (result->returnValue[0]).type);
        Equals((decimal)expected, (result->returnValue[0]).data.d);
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

        Equals(VeinTypeCode.TYPE_R8, (result->returnValue[0]).type);
        Equals((double)expected, (result->returnValue[0]).data.f);
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

        Equals(VeinTypeCode.TYPE_R8, (result->returnValue[0]).type);
        Equals(expected, (result->returnValue[0]).data.f);
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

        Equals(code, (result->returnValue[0]).type);
        Equals(5 * 5, (result->returnValue[0]).data.l);
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

        Equals(VeinTypeCode.TYPE_STRING, (result->returnValue[0]).type);

        var obj = (IshtarObject*) result->returnValue[0].data.p;
        Equals(targetStr, IshtarMarshal.ToDotnetString(obj, null));
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


        Equals(142, result->returnValue[0].data.l);
    }


    #region Complex Arithmetic TestCaseSource

    static IEnumerable<TestCaseData> ArithmeticInt32Cases()
    {
        var cases = new (int a, int b, OpCodeValue op, int expected, string label)[]
        {
            (100, 25, OpCodeValue.ADD, 125, "100+25"),
            (100, 25, OpCodeValue.SUB, 75, "100-25"),
            (7, 8, OpCodeValue.MUL, 56, "7*8"),
            (100, 4, OpCodeValue.DIV, 25, "100/4"),
            (17, 5, OpCodeValue.MOD, 2, "17%5"),
            (-10, 3, OpCodeValue.ADD, -7, "-10+3"),
            (-10, 3, OpCodeValue.SUB, -13, "-10-3"),
            (-10, 3, OpCodeValue.MUL, -30, "-10*3"),
            (0, 1, OpCodeValue.ADD, 1, "0+1"),
            (0, 1, OpCodeValue.SUB, -1, "0-1"),
            (int.MaxValue, 0, OpCodeValue.ADD, int.MaxValue, "MaxValue+0"),
            (12345, 100, OpCodeValue.MUL, 1234500, "12345*100"),
            (99, 10, OpCodeValue.DIV, 9, "99/10 truncate"),
            (99, 10, OpCodeValue.MOD, 9, "99%10"),
            (-15, -3, OpCodeValue.DIV, 5, "-15/-3"),
            (-15, -3, OpCodeValue.MOD, 0, "-15%-3"),
            (1, -1, OpCodeValue.MUL, -1, "1*-1"),
        };

        foreach (var (a, b, op, expected, label) in cases)
            yield return new TestCaseData(a, b, op, expected).SetName($"I32_{label}");
    }

    static IEnumerable<TestCaseData> ArithmeticInt64Cases()
    {
        var cases = new (long a, long b, OpCodeValue op, long expected, string label)[]
        {
            (3000000000L, 2000000000L, OpCodeValue.ADD, 5000000000L, "3B+2B"),
            (3000000000L, 2000000000L, OpCodeValue.SUB, 1000000000L, "3B-2B"),
            (100000L, 100000L, OpCodeValue.MUL, 10000000000L, "100K*100K"),
            (9000000000L, 3L, OpCodeValue.DIV, 3000000000L, "9B/3"),
            (9000000007L, 3L, OpCodeValue.MOD, 1L, "9B%3"),
            (-1L, 1L, OpCodeValue.ADD, 0L, "-1+1"),
            (long.MaxValue, 0L, OpCodeValue.ADD, long.MaxValue, "MaxValue+0"),
            (0L, long.MaxValue, OpCodeValue.SUB, -long.MaxValue, "0-MaxValue"),
        };

        foreach (var (a, b, op, expected, label) in cases)
            yield return new TestCaseData(a, b, op, expected).SetName($"I64_{label}");
    }

    static IEnumerable<TestCaseData> ArithmeticFloatCases()
    {
        var cases = new (float a, float b, OpCodeValue op, float expected, string label)[]
        {
            (1.5f, 2.5f, OpCodeValue.ADD, 4f, "1.5+2.5"),
            (10.0f, 3.0f, OpCodeValue.SUB, 7f, "10-3"),
            (2.5f, 4.0f, OpCodeValue.MUL, 10f, "2.5*4"),
            (10.0f, 4.0f, OpCodeValue.DIV, 2.5f, "10/4"),
            (0.1f, 0.2f, OpCodeValue.ADD, 0.3f, "0.1+0.2 approx"),
            (-1.5f, 2.0f, OpCodeValue.MUL, -3.0f, "-1.5*2"),
            (7.5f, 2.5f, OpCodeValue.MOD, 0f, "7.5%2.5"),
            (100f, 3f, OpCodeValue.DIV, 100f / 3f, "100/3"),
            (float.MaxValue, 0f, OpCodeValue.ADD, float.MaxValue, "MaxValue+0"),
        };

        foreach (var (a, b, op, expected, label) in cases)
            yield return new TestCaseData(a, b, op, expected).SetName($"F32_{label}");
    }

    static IEnumerable<TestCaseData> ArithmeticDoubleCases()
    {
        var cases = new (double a, double b, OpCodeValue op, double expected, string label)[]
        {
            (1.5, 2.5, OpCodeValue.ADD, 4.0, "1.5+2.5"),
            (100.0, 33.0, OpCodeValue.SUB, 67.0, "100-33"),
            (2.5, 4.0, OpCodeValue.MUL, 10.0, "2.5*4"),
            (10.0, 3.0, OpCodeValue.DIV, 10.0 / 3.0, "10/3"),
            (10.0, 3.0, OpCodeValue.MOD, 1.0, "10%3"),
            (1e15, 1e15, OpCodeValue.ADD, 2e15, "1e15+1e15"),
            (-1.0, -1.0, OpCodeValue.MUL, 1.0, "-1*-1"),
            (0.0, 42.0, OpCodeValue.SUB, -42.0, "0-42"),
            (double.MaxValue, 0.0, OpCodeValue.ADD, double.MaxValue, "MaxValue+0"),
        };

        foreach (var (a, b, op, expected, label) in cases)
            yield return new TestCaseData(a, b, op, expected).SetName($"F64_{label}");
    }

    static IEnumerable<TestCaseData> ArithmeticDecimalCases()
    {
        var cases = new (decimal a, decimal b, OpCodeValue op, decimal expected, string label)[]
        {
            (1.5m, 2.5m, OpCodeValue.ADD, 4.0m, "1.5+2.5"),
            (100m, 33m, OpCodeValue.SUB, 67m, "100-33"),
            (2.5m, 4m, OpCodeValue.MUL, 10m, "2.5*4"),
            (10m, 4m, OpCodeValue.DIV, 2.5m, "10/4"),
            (10m, 3m, OpCodeValue.MOD, 1m, "10%3"),
            (999999999999m, 1m, OpCodeValue.ADD, 1000000000000m, "big+1"),
            (-50m, 25m, OpCodeValue.DIV, -2m, "-50/25"),
            (0.001m, 0.002m, OpCodeValue.ADD, 0.003m, "precision"),
        };

        foreach (var (a, b, op, expected, label) in cases)
            yield return new TestCaseData(a, b, op, expected).SetName($"R16_{label}");
    }

    static IEnumerable<TestCaseData> ArithmeticChainedCases()
    {
        // (a op1 b) op2 c — chained operations
        yield return new TestCaseData(10, 3, 2, OpCodeValue.ADD, OpCodeValue.MUL, 26)
            .SetName("Chain_(10+3)*2");
        yield return new TestCaseData(20, 4, 5, OpCodeValue.DIV, OpCodeValue.ADD, 10)
            .SetName("Chain_(20/4)+5");
        yield return new TestCaseData(7, 3, 2, OpCodeValue.MUL, OpCodeValue.SUB, 19)
            .SetName("Chain_(7*3)-2");
        yield return new TestCaseData(100, 10, 3, OpCodeValue.MOD, OpCodeValue.ADD, 3)
            .SetName("Chain_(100%10)+3");
        yield return new TestCaseData(5, 5, 5, OpCodeValue.ADD, OpCodeValue.ADD, 15)
            .SetName("Chain_(5+5)+5");
        yield return new TestCaseData(50, 2, 2, OpCodeValue.DIV, OpCodeValue.DIV, 12)
            .SetName("Chain_(50/2)/2");
    }

    [Test]
    [TestCaseSource(nameof(ArithmeticInt32Cases))]
    [Parallelizable(ParallelScope.None)]
    public unsafe void Arithmetic_Complex_I32(int a, int b, OpCodeValue op, long expected)
    {
        using var scope = CreateScope();

        scope.OnCodeBuild((gen, _) => {
            gen.Emit(OpCodes.LDC_I4_S, a);
            gen.Emit(OpCodes.LDC_I4_S, b);
            gen.Emit(OpCodes.all[op]);
            gen.Emit(OpCodes.RET);
        });

        var result = scope
            .Compile()
            .Execute()
            .Validate();

        Equals(VeinTypeCode.TYPE_I4, result->returnValue[0].type);
        Equals(expected, (long)result->returnValue[0].data.i);
    }

    [Test]
    [TestCaseSource(nameof(ArithmeticInt64Cases))]
    [Parallelizable(ParallelScope.None)]
    public unsafe void Arithmetic_Complex_I64(long a, long b, OpCodeValue op, long expected)
    {
        using var scope = CreateScope();

        scope.OnCodeBuild((gen, _) => {
            gen.Emit(OpCodes.LDC_I8_S, a);
            gen.Emit(OpCodes.LDC_I8_S, b);
            gen.Emit(OpCodes.all[op]);
            gen.Emit(OpCodes.RET);
        });

        var result = scope
            .Compile()
            .Execute()
            .Validate();

        Equals(VeinTypeCode.TYPE_I8, result->returnValue[0].type);
        Equals(expected, result->returnValue[0].data.l);
    }

    [Test]
    [TestCaseSource(nameof(ArithmeticFloatCases))]
    [Parallelizable(ParallelScope.None)]
    public unsafe void Arithmetic_Complex_F32(float a, float b, OpCodeValue op, float expected)
    {
        using var scope = CreateScope();

        scope.OnCodeBuild((gen, _) => {
            gen.Emit(OpCodes.LDC_F4, a);
            gen.Emit(OpCodes.LDC_F4, b);
            gen.Emit(OpCodes.all[op]);
            gen.Emit(OpCodes.RET);
        });

        var result = scope
            .Compile()
            .Execute()
            .Validate();

        Equals(VeinTypeCode.TYPE_R4, result->returnValue[0].type);
        Equals(expected, result->returnValue[0].data.f_r4);
    }

    [Test]
    [TestCaseSource(nameof(ArithmeticDoubleCases))]
    [Parallelizable(ParallelScope.None)]
    public unsafe void Arithmetic_Complex_F64(double a, double b, OpCodeValue op, double expected)
    {
        using var scope = CreateScope();

        scope.OnCodeBuild((gen, _) => {
            gen.Emit(OpCodes.LDC_F8, a);
            gen.Emit(OpCodes.LDC_F8, b);
            gen.Emit(OpCodes.all[op]);
            gen.Emit(OpCodes.RET);
        });

        var result = scope
            .Compile()
            .Execute()
            .Validate();

        Equals(VeinTypeCode.TYPE_R8, result->returnValue[0].type);
        Equals(expected, result->returnValue[0].data.f);
    }

    [Test]
    [TestCaseSource(nameof(ArithmeticDecimalCases))]
    [Parallelizable(ParallelScope.None)]
    public unsafe void Arithmetic_Complex_R16(decimal a, decimal b, OpCodeValue op, decimal expected)
    {
        using var scope = CreateScope();

        scope.OnCodeBuild((gen, _) => {
            gen.Emit(OpCodes.LDC_F16, a);
            gen.Emit(OpCodes.LDC_F16, b);
            gen.Emit(OpCodes.all[op]);
            gen.Emit(OpCodes.RET);
        });

        var result = scope
            .Compile()
            .Execute()
            .Validate();

        Equals(VeinTypeCode.TYPE_R16, result->returnValue[0].type);
        Equals(expected, result->returnValue[0].data.d);
    }

    [Test]
    [TestCaseSource(nameof(ArithmeticChainedCases))]
    [Parallelizable(ParallelScope.None)]
    public unsafe void Arithmetic_Chained_I32(int a, int b, int c, OpCodeValue op1, OpCodeValue op2, long expected)
    {
        using var scope = CreateScope();

        scope.OnCodeBuild((gen, _) => {
            gen.Emit(OpCodes.LDC_I4_S, a);
            gen.Emit(OpCodes.LDC_I4_S, b);
            gen.Emit(OpCodes.all[op1]);
            gen.Emit(OpCodes.LDC_I4_S, c);
            gen.Emit(OpCodes.all[op2]);
            gen.Emit(OpCodes.RET);
        });

        var result = scope
            .Compile()
            .Execute()
            .Validate();

        Equals(VeinTypeCode.TYPE_I4, result->returnValue[0].type);
        Equals(expected, (long)result->returnValue[0].data.i);
    }

    #endregion
}
