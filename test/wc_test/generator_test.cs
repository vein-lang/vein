namespace wc_test
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using wave;
    using wave.emit;
    using wave.extensions;
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
                " class Program { void main() { if(ze()) return x; else { return d();  } } }");

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
        public void ReturnStatementCompilation1()
        {
            var ret = new ReturnStatementSyntax
            {
                Expression = new SingleLiteralExpressionSyntax(14.3f)
            };


            var actual = CreateGenerator();
            
            actual.EmitReturn(ret);
            
            var expected = CreateGenerator();

            expected.Emit(OpCodes.LDC_F4, 14.3f);
            expected.Emit(OpCodes.RET);
            
            
            Assert.Equal(expected.BakeByteArray(), actual.BakeByteArray());
        }
        
        [Fact]
        public void ReturnStatementCompilation2()
        {
            var ret = new ReturnStatementSyntax
            {
                Expression = new ExpressionSyntax("x")
            };


            var actual = CreateGenerator(("x", WaveTypeCode.TYPE_STRING));
            
            actual.EmitReturn(ret);
            
            var expected = CreateGenerator(("x", WaveTypeCode.TYPE_STRING));

            expected.Emit(OpCodes.LDF, new FieldName("x"));
            expected.Emit(OpCodes.RET);
            
            
            Assert.Equal(expected.BakeByteArray(), actual.BakeByteArray());
        }
        
        [Fact]
        public void ReturnStatementCompilation3()
        {
            var ret = new ReturnStatementSyntax
            {
                Expression = new ExpressionSyntax("x")
            };
            
            var actual = CreateGenerator();
            
            Assert.Throws<FieldIsNotDeclaredException>(() => actual.EmitReturn(ret));
        }
        
        
        public static ILGenerator CreateGenerator(params WaveArgumentRef[] args)
        {
            var module = new ModuleBuilder(Guid.NewGuid().ToString());
            var @class = new ClassBuilder(module, "foo", "bar");
            var method = @class.DefineMethod("foo", args);
            return method.GetGenerator();
        }
    }
}