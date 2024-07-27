namespace veinc_test;

public class il_test
{
    [Test]
    public void DeconstructOpcodes1()
    {
        var gen = CreateGenerator();


        gen.Emit(OpCodes.ADD);
        gen.Emit(OpCodes.DIV);
        gen.Emit(OpCodes.LDARG_1);
        var (result, _) = ILReader.Deconstruct(gen.BakeByteArray(), "");


        Assert.AreEqual(OpCodes.ADD.Value, result[0]);
        Assert.AreEqual(OpCodes.DIV.Value, result[1]);
        Assert.AreEqual(OpCodes.LDARG_1.Value, result[2]);
    }
    [Test, Ignore("MANUAL")]
    public void DeconstructOpcodes2()
    {
        var gen = CreateGenerator();


        gen.Emit(OpCodes.LDC_I4_S, 1448);
        gen.Emit(OpCodes.LDC_I4_S, 228);
        var (result, _) = ILReader.Deconstruct(gen.BakeByteArray(), "");


        Assert.AreEqual(OpCodes.LDC_I4_S.Value, result[0]);
        Assert.AreEqual((uint)1448, result[1]);
        Assert.AreEqual(OpCodes.LDC_I4_S.Value, result[2]);
        Assert.AreEqual((uint)228, result[3]);
    }

    [Test, Ignore("MANUAL")]
    public unsafe void LocalsGeneratorTest()
    {
        var gen = CreateGenerator();

        gen.Emit(OpCodes.RET);
        gen.Emit(OpCodes.AND);
        gen.EnsureLocal("foo1", VeinTypeCode.TYPE_I8.AsClass()(Types.Storage));
        gen.Emit(OpCodes.LDC_I8_3);
        gen.Emit(OpCodes.STLOC_0);
        gen.EnsureLocal("foo2", VeinTypeCode.TYPE_I4.AsClass()(Types.Storage));
        gen.Emit(OpCodes.LDC_I4_3);
        gen.Emit(OpCodes.STLOC_1);


        var str = gen.BakeDebugString();
        var bytes = gen.BakeByteArray();
        var (result, _) = ILReader.Deconstruct(bytes, "");
    }


    [Test]
    public unsafe void DeconstructOpcodes3()
    {
        var gen = CreateGenerator();

        var l1 = gen.DefineLabel("1");
        var l2 = gen.DefineLabel("2");
        gen.Emit(OpCodes.ADD);
        gen.Emit(OpCodes.LDC_I4_S, 228);
        gen.Emit(OpCodes.ADD);
        gen.Emit(OpCodes.JMP_HQ, l2);
        gen.Emit(OpCodes.LDC_I4_S, 228);
        gen.Emit(OpCodes.ADD);
        gen.Emit(OpCodes.LDC_I4_S, 228);
        gen.Emit(OpCodes.LDC_I4_S, 228);
        gen.Emit(OpCodes.ADD);
        gen.Emit(OpCodes.JMP_HQ, l1);
        gen.UseLabel(l1);
        gen.Emit(OpCodes.SUB);
        gen.Emit(OpCodes.SUB);
        gen.UseLabel(l2);
        gen.Emit(OpCodes.SUB);
        gen.Emit(OpCodes.SUB);
        var offset = 0;
        var body = gen.BakeByteArray();
        var (result, map) = ILReader.Deconstruct(body, &offset, null);
        var labels = ILReader.DeconstructLabels(body, &offset);

        var first_label = result[map[labels[0]].pos];

        Assert.AreEqual(first_label, OpCodes.SUB.Value);
        //Assert.AreEqual(second_label, OpCodes.SUB.Value);
        Assert.AreEqual(OpCodes.ADD.Value, result[0]);
        Assert.AreEqual(OpCodes.LDC_I4_S.Value, result[1]);
        Assert.AreEqual((uint)228, result[2]);
    }


    public static ILGenerator CreateGenerator(params VeinArgumentRef[] args)
    {
        var module = new VeinModuleBuilder(new ModuleNameSymbol(Guid.NewGuid().ToString()), (Types.Storage));
        var @class = new ClassBuilder(module, new QualityTypeName(new NameSymbol("bar"), NamespaceSymbol.Internal, module.Name)); 
        var method = @class.DefineMethod("foo", VeinTypeCode.TYPE_VOID.AsClass()(Types.Storage), args);
        return method.GetGenerator();
    }
}
