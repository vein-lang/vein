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
            foreach (var i in Enumerable.Range(0, 10))
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

    #region Physics/Engineering Formula Tests

    // These tests simulate real-world formulas using multi-step stack operations.
    // Instructions are interleaved: double = LDC_F8, OpCodeValue = arithmetic op.
    // This is how a real stack machine encodes expressions.

    static IEnumerable<TestCaseData> PhysicsFormulaCases_F64()
    {
        // Ohm's law: V = I * R
        // I=2.5A, R=100Ω → V=250V
        yield return new TestCaseData(
            new object[] { 2.5, 100.0, OpCodeValue.MUL },
            250.0
        ).SetName("Ohm_V=IR");

        // Power: P = V² / R = V * V / R
        // V=220, R=100 → P=484
        yield return new TestCaseData(
            new object[] { 220.0, 220.0, OpCodeValue.MUL, 100.0, OpCodeValue.DIV },
            484.0
        ).SetName("Power_P=V²/R");

        // Electrical energy: E = P * t
        // P=1500W, t=3600s → E=5400000J
        yield return new TestCaseData(
            new object[] { 1500.0, 3600.0, OpCodeValue.MUL },
            5400000.0
        ).SetName("Energy_E=Pt");

        // Series resistance: R_total = R1 + R2 + R3
        // R1=10, R2=20, R3=30 → 60
        yield return new TestCaseData(
            new object[] { 10.0, 20.0, OpCodeValue.ADD, 30.0, OpCodeValue.ADD },
            60.0
        ).SetName("Resistance_Series_R1+R2+R3");

        // Gravitational force: F = G * m1 * m2 / r²
        // G=6.674, m1=100, m2=200, r=10
        yield return new TestCaseData(
            new object[] { 6.674, 100.0, OpCodeValue.MUL, 200.0, OpCodeValue.MUL, 10.0, 10.0, OpCodeValue.MUL, OpCodeValue.DIV },
            6.674 * 100.0 * 200.0 / (10.0 * 10.0)
        ).SetName("Gravity_F=Gm1m2/r²");

        // Kinetic energy: KE = m * v * v / 2
        // m=10kg, v=3m/s → KE = 45
        yield return new TestCaseData(
            new object[] { 10.0, 3.0, 3.0, OpCodeValue.MUL, OpCodeValue.MUL, 2.0, OpCodeValue.DIV },
            45.0
        ).SetName("KineticEnergy_KE=mv²/2");

        // Potential energy: PE = m * g * h
        // m=5kg, g=9.81, h=20m
        yield return new TestCaseData(
            new object[] { 5.0, 9.81, OpCodeValue.MUL, 20.0, OpCodeValue.MUL },
            5.0 * 9.81 * 20.0
        ).SetName("PotentialEnergy_PE=mgh");

        // Free fall: 2*g*h (pre-sqrt)
        // g=9.81, h=45
        yield return new TestCaseData(
            new object[] { 2.0, 9.81, OpCodeValue.MUL, 45.0, OpCodeValue.MUL },
            2.0 * 9.81 * 45.0
        ).SetName("FreeFall_2gh");

        // Coulomb's law: F = k * q1 * q2 / r²
        // k=9.0, q1=2.0, q2=3.0, r=1.5
        yield return new TestCaseData(
            new object[] { 9.0, 2.0, OpCodeValue.MUL, 3.0, OpCodeValue.MUL, 1.5, 1.5, OpCodeValue.MUL, OpCodeValue.DIV },
            9.0 * 2.0 * 3.0 / (1.5 * 1.5)
        ).SetName("Coulomb_F=kq1q2/r²");

        // Parallel resistance: R_total = (R1*R2)/(R1+R2)
        // R1=6, R2=3 → R_total = 18/9 = 2
        yield return new TestCaseData(
            new object[] { 6.0, 3.0, OpCodeValue.MUL, 6.0, 3.0, OpCodeValue.ADD, OpCodeValue.DIV },
            2.0
        ).SetName("Resistance_Parallel_(R1*R2)/(R1+R2)");

        // Momentum: p = m * v
        // m=75kg, v=12m/s → p=900
        yield return new TestCaseData(
            new object[] { 75.0, 12.0, OpCodeValue.MUL },
            900.0
        ).SetName("Momentum_p=mv");

        // Work: W = F * d
        // F=500N, d=10m → W=5000J
        yield return new TestCaseData(
            new object[] { 500.0, 10.0, OpCodeValue.MUL },
            5000.0
        ).SetName("Work_W=Fd");

        // Pressure: P = F / A
        // F=1000N, A=0.5m² → P=2000Pa
        yield return new TestCaseData(
            new object[] { 1000.0, 0.5, OpCodeValue.DIV },
            2000.0
        ).SetName("Pressure_P=F/A");

        // Density: ρ = m / V
        // m=500kg, V=0.25m³ → ρ=2000 kg/m³
        yield return new TestCaseData(
            new object[] { 500.0, 0.25, OpCodeValue.DIV },
            2000.0
        ).SetName("Density_ρ=m/V");

        // Average velocity: v_avg = (v1 + v2) / 2
        // v1=20, v2=80 → 50
        yield return new TestCaseData(
            new object[] { 20.0, 80.0, OpCodeValue.ADD, 2.0, OpCodeValue.DIV },
            50.0
        ).SetName("AvgVelocity_(v1+v2)/2");

        // Capacitor energy: E = C * V * V / 2
        // C=0.001F, V=100V → E = 5
        yield return new TestCaseData(
            new object[] { 0.001, 100.0, 100.0, OpCodeValue.MUL, OpCodeValue.MUL, 2.0, OpCodeValue.DIV },
            0.001 * 100.0 * 100.0 / 2.0
        ).SetName("CapacitorEnergy_CV²/2");

        // Transformer: V2 = V1 * N2 / N1
        // V1=220, N2=10, N1=100 → V2=22
        yield return new TestCaseData(
            new object[] { 220.0, 10.0, OpCodeValue.MUL, 100.0, OpCodeValue.DIV },
            22.0
        ).SetName("Transformer_V2=V1*N2/N1");

        // Centripetal acceleration: a = v² / r = v * v / r
        // v=30m/s, r=15m → a=60 m/s²
        yield return new TestCaseData(
            new object[] { 30.0, 30.0, OpCodeValue.MUL, 15.0, OpCodeValue.DIV },
            60.0
        ).SetName("Centripetal_a=v²/r");

        // Heat transfer: Q = m * c * ΔT
        // m=2kg, c=4186 J/(kg·K), ΔT=50K → Q=418600
        yield return new TestCaseData(
            new object[] { 2.0, 4186.0, OpCodeValue.MUL, 50.0, OpCodeValue.MUL },
            2.0 * 4186.0 * 50.0
        ).SetName("HeatTransfer_Q=mcΔT");
    }

    [Test]
    [TestCaseSource(nameof(PhysicsFormulaCases_F64))]
    [Parallelizable(ParallelScope.None)]
    public unsafe void Arithmetic_Physics_F64(object[] instructions, double expected)
    {
        using var scope = CreateScope();

        scope.OnCodeBuild((gen, _) => {
            foreach (var instr in instructions)
            {
                if (instr is double d)
                    gen.Emit(OpCodes.LDC_F8, d);
                else if (instr is OpCodeValue op)
                    gen.Emit(OpCodes.all[op]);
            }

            gen.Emit(OpCodes.RET);
        });

        var result = scope
            .Compile()
            .Execute()
            .Validate();

        Equals(VeinTypeCode.TYPE_R8, result->returnValue[0].type);
        Equals(expected, result->returnValue[0].data.f);
    }

    #endregion
}
