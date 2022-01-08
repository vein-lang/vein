namespace ishtar_test;

[TestFixture]
public unsafe class ReflectionTest : IshtarTestBase
{
    [Test]
    public void AllocateType()
    {
        var ctx = CreateContext();
        var frame = ctx.Execute((generator, o) => generator.Emit(OpCodes.RET));
        var obj = IshtarGC.AllocTypeInfoObject(T.STRING, frame);
        IshtarGC.FreeObject(&obj, frame);
    }

    [Test]
    public void AllocateField()
    {
        var ctx = CreateContext();
        var frame = ctx.Execute((generator, o) => generator.Emit(OpCodes.RET));
        var obj = IshtarGC.AllocFieldInfoObject(T.STRING.Field["Length"], frame);
        new IshtarLayerField(obj, frame);
        IshtarGC.FreeObject(&obj, frame);
    }

    [Test]
    public void AllocateMethod()
    {
        var ctx = CreateContext();
        var frame = ctx.Execute((generator, o) => generator.Emit(OpCodes.RET));
        var obj = IshtarGC.AllocMethodInfoObject(T.STRING.Method["op_Equal(String,String)"], frame);
        var fn = new IshtarLayerFunction(obj, frame);
        Assert.AreEqual("op_Equal", fn.Name);
        IshtarGC.FreeObject(&obj, frame);
    }
}
