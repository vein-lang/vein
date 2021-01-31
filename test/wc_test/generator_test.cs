namespace wc_test
{
    using System.IO;
    using wave;
    using wave.emit;
    using wave.fs;
    using Xunit;

    public class generator_test
    {
        [Fact]
        public void Test()
        {
            var module = new ModuleBuilder("xuy");
            var clazz = module.DefineClass("svack_pidars", "wave/lang");
            clazz.SetFlags(ClassFlags.Public | ClassFlags.Static);
            var method = clazz.DefineMethod("insert_dick_into_svack");
            method.SetFlags(MethodFlags.Public | MethodFlags.Static);
            var body = method.GetGenerator();
            
            body.Emit(OpCodes.LDC_I4_S, 1448);
            body.Emit(OpCodes.LDC_I4_S, 228);
            body.Emit(OpCodes.ADD);
            body.Emit(OpCodes.LDC_I4_S, 2);
            body.Emit(OpCodes.XOR);
            body.Emit(OpCodes.DUMP_0);
            body.Emit(OpCodes.RET);

            method.BakeByteArray();
            
            
            File.WriteAllText(@"C:\Users\ls-mi\Desktop\wave.il", module.BakeDebugString());

        }

        [Fact]
        public void TestIL()
        {
            var module = new ModuleBuilder("xuy");
            var clazz = module.DefineClass("svack_pidars", "wave/lang");
            clazz.SetFlags(ClassFlags.Public | ClassFlags.Static);
            var method = clazz.DefineMethod("insert_dick_into_svack");
            method.SetFlags(MethodFlags.Public | MethodFlags.Static);
            var body = method.GetGenerator();
            
            body.Emit(OpCodes.LDC_I4_S, 1448);
            body.Emit(OpCodes.LDC_I4_S, 228);
            body.Emit(OpCodes.ADD);
            body.Emit(OpCodes.LDC_I4_S, 2);
            body.Emit(OpCodes.XOR);
            body.Emit(OpCodes.DUMP_0);
            body.Emit(OpCodes.RET);


            var body_module = module.BakeByteArray();


            var asm = new WaveAssembly();
            
            asm.AddSegment((".code", body_module));
            
            WaveAssembly.WriteToFile(asm, @"C:\Users\ls-mi\Desktop\wave.dll");
        }
    }
}