namespace wc_test
{
    using System;
    using ishtar;
    using mana.ishtar.emit;
    using mana.runtime;
    using Xunit;

    public class il_test
    {
        [Fact]
        public void DeconstructOpcodes1()
        {
            var gen = CreateGenerator();


            gen.Emit(OpCodes.ADD);
            gen.Emit(OpCodes.DIV);
            gen.Emit(OpCodes.LDARG_0);
            var (result, _) = ILReader.Deconstruct(gen.BakeByteArray(), null);


            Assert.Equal(OpCodes.ADD.Value, result[0]);
            Assert.Equal(OpCodes.DIV.Value, result[1]);
            Assert.Equal(OpCodes.LDARG_0.Value, result[2]);
        }
        [Fact(Skip = "MANUAL")]
        public void DeconstructOpcodes2()
        {
            var gen = CreateGenerator();


            gen.Emit(OpCodes.LDC_I4_S, 1448);
            gen.Emit(OpCodes.LDC_I4_S, 228);
            var (result, _) = ILReader.Deconstruct(gen.BakeByteArray(), null);


            Assert.Equal(OpCodes.LDC_I4_S.Value, result[0]);
            Assert.Equal((uint)1448, result[1]);
            Assert.Equal(OpCodes.LDC_I4_S.Value, result[2]);
            Assert.Equal((uint)228, result[3]);
        }

        [Fact]
        public unsafe void LocalsGeneratorTest()
        {
            var gen = CreateGenerator();

            gen.Emit(OpCodes.RET);
            gen.Emit(OpCodes.AND);
            gen.EnsureLocal("foo1", ManaTypeCode.TYPE_I8.AsClass());
            gen.Emit(OpCodes.LDC_I8_3);
            gen.Emit(OpCodes.STLOC_0);
            gen.EnsureLocal("foo2", ManaTypeCode.TYPE_I4.AsClass());
            gen.Emit(OpCodes.LDC_I4_3);
            gen.Emit(OpCodes.STLOC_1);


            var str = gen.BakeDebugString();
            var bytes = gen.BakeByteArray();
            var (result, _) = ILReader.Deconstruct(bytes, null);
        }


        [Fact]
        public unsafe void DeconstructOpcodes3()
        {
            var gen = CreateGenerator();

            var l1 = gen.DefineLabel();
            var l2 = gen.DefineLabel();
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
            var labels = ILReader.DeconstructLabels(body, offset);

            var first_label = result[map[labels[0]].pos];

            Assert.Equal(first_label, OpCodes.SUB.Value);
            //Assert.Equal(second_label, OpCodes.SUB.Value);
            Assert.Equal(OpCodes.ADD.Value, result[0]);
            Assert.Equal(OpCodes.LDC_I4_S.Value, result[1]);
            Assert.Equal((uint)228, result[2]);
        }


        public static ILGenerator CreateGenerator(params ManaArgumentRef[] args)
        {
            var module = new ManaModuleBuilder(Guid.NewGuid().ToString());
            var @class = new ClassBuilder(module, $"{module.Name}%global::foo/bar");
            var method = @class.DefineMethod("foo", ManaTypeCode.TYPE_VOID.AsClass(), args);
            return method.GetGenerator();
        }
    }
}
