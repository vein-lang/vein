namespace wc_test
{
    using System.IO;
    using wave;
    using wave.emit;
    using wave.fs;
    using wave.stl;
    using wave.syntax;
    using Xunit;

    public class generator_test
    {
        [Fact]
        public void Test()
        {
            var module = new ModuleBuilder("xuy");
            var clazz = module.DefineClass("svack_pidars", "wave/lang");
            clazz.SetFlags(ClassFlags.Public | ClassFlags.Static);
            var method = clazz.DefineMethod("insert_dick_into_svack", ("x", WaveTypeCode.TYPE_STRING));
            method.SetFlags(MethodFlags.Public | MethodFlags.Static);
            var body = method.GetGenerator();
            
            body.Emit(OpCodes.LDC_I4_S, 1448);
            body.Emit(OpCodes.LDC_I4_S, 228);
            body.Emit(OpCodes.ADD);
            body.Emit(OpCodes.LDC_I4_S, 2);
            body.Emit(OpCodes.XOR);
            body.Emit(OpCodes.DUMP_0);
            body.Emit(OpCodes.LDF, new FieldName("x"));
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
            var method = clazz.DefineMethod("insert_dick_into_svack", ("x", WaveTypeCode.TYPE_STRING));
            method.SetFlags(MethodFlags.Public | MethodFlags.Static);
            var body = method.GetGenerator();
            
            body.Emit(OpCodes.LDC_I4_S, 1448);
            body.Emit(OpCodes.LDC_I4_S, 228);
            body.Emit(OpCodes.ADD);
            body.Emit(OpCodes.LDC_I4_S, 2);
            body.Emit(OpCodes.XOR);
            body.Emit(OpCodes.DUMP_0);
            body.Emit(OpCodes.LDF, "x");
            body.Emit(OpCodes.RET);


            var body_module = module.BakeByteArray();


            var asm = new WaveAssembly();
            
            asm.AddSegment((".code", body_module));
            
            WaveAssembly.WriteToFile(asm, @"C:\Users\ls-mi\Desktop\wave.dll");
        }
        [Fact]
        public void AST2ILTest()
        {
            var w = new WaveSyntax();
            var ast = w.CompilationUnit.ParseWave(
                " class Program { void main() { return x; } }");

            var module = new ModuleBuilder("foo");

            foreach (var member in ast.Members)
            {
                if (member is ClassDeclarationSyntax classMember)
                {
                    var @class = module.DefineClass(classMember.Identifier, "wave/lang");

                    foreach (var methodMember in classMember.Methods)
                    {
                        var method = @class.DefineMethod(methodMember.Identifier);
                        var generator = method.GetGenerator();

                        foreach (var statement in methodMember.Body.Statements)
                        {
                            var st = statement;
                        }
                    }
                }
            }
        }
        [Fact]
        public void StatementCompilation()
        {
            var ret = new ReturnStatementSyntax
            {
                Expression = new SingleLiteralExpressionSyntax(14.3f)
            };
            
            
        }
    }
    
    public class ClassContext
    {
        private readonly ClassBuilder _builder;

        public ClassContext(ClassBuilder builder) => _builder = builder;
    }
    
   
}