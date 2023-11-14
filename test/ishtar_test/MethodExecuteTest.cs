namespace ishtar_test;

using static MethodFlags;

[TestFixture]
public unsafe class MethodExecuteTest : IshtarTestBase
{
    [Test]
    [Parallelizable(ParallelScope.None)]
    public void S0()
    {
        using var ctx = CreateContext();

        ctx.OnClassBuild((x, y) =>
        {
            var method = x.DefineMethod("foo", Public | Static, VeinTypeCode.TYPE_I4.AsClass()(Types));
            method.GetGenerator()
                .Emit(OpCodes.LDC_I4_S, 225)
                .Emit(OpCodes.RET);
            y.m = method;
        });


        var result = ctx.Execute((x, y) =>
        {
            x.Emit(OpCodes.CALL, (MethodBuilder)y.m);
            x.Emit(OpCodes.LDC_I4_2);
            x.Emit(OpCodes.MUL);
            x.Emit(OpCodes.RET);
        });

        Assert.AreEqual(result.returnValue->data.i, 225 * 2);
    }

    [Test]
    [Parallelizable(ParallelScope.None)]
    public void S1()
    {
        using var ctx = CreateContext();

        ctx.OnClassBuild((x, y) =>
        {
            var method = x.DefineMethod("foo", Public | Static, VeinTypeCode.TYPE_I4.AsClass()(Types),
                new VeinArgumentRef("value", VeinTypeCode.TYPE_I4.AsClass()(Types)));
            method.GetGenerator()
                .Emit(OpCodes.LDARG_0)
                .Emit(OpCodes.LDARG_S, 0)
                .Emit(OpCodes.MUL)
                .Emit(OpCodes.RET);
            y.m = method;
        });


        var result = ctx.Execute((x, y) =>
        {
            x.Emit(OpCodes.LDC_I4_2);
            x.Emit(OpCodes.CALL, (MethodBuilder)y.m);
            x.Emit(OpCodes.LDC_I4_2);
            x.Emit(OpCodes.MUL);
            x.Emit(OpCodes.RET);
        });

        Assert.AreEqual(result.returnValue->data.i, 2 * 2 * 2);
    }

    [Test, Ignore("LDNULL cause crash ishtar.")]
    [Parallelizable(ParallelScope.None)]
    public void S2()
    {
        using var ctx = CreateContext();

        ctx.OnClassBuild((x, y) =>
        {
            var method = x.DefineMethod("foo", Public | Static, VeinTypeCode.TYPE_STRING.AsClass()(Types));
            method.GetGenerator()
                .Emit(OpCodes.LDNULL)
                .Emit(OpCodes.LDC_STR, "foo")
                .Emit(OpCodes.RET);
            y.method = method;
        });


        var result = ctx.Execute((x, y) =>
        {
            x.Emit(OpCodes.RESERVED_2);
            x.Emit(OpCodes.CALL, (MethodBuilder)y.method);
            x.Emit(OpCodes.RET);
        });

        Assert.AreEqual(result.last_ip, OpCodeValue.RET);
    }

}
