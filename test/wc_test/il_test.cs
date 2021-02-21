namespace wc_test
{
    using System;
    using wave;
    using wave.emit;
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
            var result = ILReader.Deconstruct(gen.BakeByteArray());
            
            
            Assert.Equal(OpCodes.ADD.Value, result[0]);
            Assert.Equal(OpCodes.DIV.Value, result[1]);
            Assert.Equal(OpCodes.LDARG_0.Value, result[2]);
        }
        [Fact]
        public void DeconstructOpcodes2()
        {
            var gen = CreateGenerator();
            
            
            gen.Emit(OpCodes.LDC_I4_S, 1448);
            gen.Emit(OpCodes.LDC_I4_S, 228);
            var result = ILReader.Deconstruct(gen.BakeByteArray());
            
            
            Assert.Equal(OpCodes.LDC_I4_S.Value, result[0]);
            Assert.Equal((uint)1448, result[1]);
            Assert.Equal(OpCodes.LDC_I4_S.Value, result[2]);
            Assert.Equal((uint)228, result[3]);
        }
        
        
        public static ILGenerator CreateGenerator(params WaveArgumentRef[] args)
        {
            var module = new ModuleBuilder(Guid.NewGuid().ToString());
            var @class = new ClassBuilder(module, "foo/bar");
            var method = @class.DefineMethod("foo", WaveTypeCode.TYPE_VOID.AsType(), args);
            return method.GetGenerator();
        }
    }
}