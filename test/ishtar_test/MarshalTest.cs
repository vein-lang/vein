namespace ishtar_test;

using ishtar;
using NUnit.Framework;

public unsafe class MarshalTest : IshtarTestBase
{
    [Test]
    [Parallelizable(ParallelScope.None)]
    public void Int16Test()
    {
        using var scope = CreateScope();
        var ctx = scope.Compile();
        var GC = ctx.VM->gc;

        short clr = short.MaxValue / 2;

        var v = GC->ToIshtarObject(clr, ctx.VM->Frames->EntryPoint);
        var r = IshtarMarshal.ToDotnetInt16(v, null);

        Equals(clr, r);
    }
    [Test]
    [Parallelizable(ParallelScope.None)]
    public void Int32Test()
    {
        using var scope = CreateScope();
        var ctx = scope.Compile();
        var GC = ctx.VM->gc;

        int clr = int.MaxValue / 2;

        var v = GC->ToIshtarObject(clr, ctx.VM->Frames->EntryPoint);
        var r = IshtarMarshal.ToDotnetInt32(v, null);

        Equals(clr, r);
    }
    [Test]
    [Parallelizable(ParallelScope.None)]
    public void Int64Test()
    {
        using var scope = CreateScope();
        var ctx = scope.Compile();
        var GC = ctx.VM->gc;

        long clr = long.MaxValue / 2;

        var v = GC->ToIshtarObject(clr, ctx.VM->Frames->EntryPoint);
        var r = IshtarMarshal.ToDotnetInt64(v, null);

        Equals(clr, r);
    }

    [Test]
    [Parallelizable(ParallelScope.None)]
    public void StringTest()
    {
        using var scope = CreateScope();
        var ctx = scope.Compile();
        var GC = ctx.VM->gc;

        var clr = "long.MaxValue / 2";

        var v = GC->ToIshtarObject(clr, ctx.VM->Frames->EntryPoint);
        var r = IshtarMarshal.ToDotnetString(v, null);

        Equals(clr, r);
    }
}
