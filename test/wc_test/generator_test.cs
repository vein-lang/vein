namespace wc_test
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using ishtar;
    using Spectre.Console;
    using wave.fs;
    using wave.ishtar.emit;
    using wave.runtime;
    using wave.stl;
    using wave.syntax;
    using Xunit;
    using Xunit.Abstractions;
    
    public class generator_test
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public generator_test(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        [Fact(Skip = "MANUAL")]
        public void Test()
        {
            var module = new WaveModuleBuilder("xuy");
            var clazz = module.DefineClass("xuy%global::wave/lang/svack_pidars");
            clazz.Flags = ClassFlags.Public | ClassFlags.Static;
            var method = clazz.DefineMethod("insert_dick_into_svack", MethodFlags.Public,WaveTypeCode.TYPE_VOID.AsClass(), ("x", WaveTypeCode.TYPE_STRING));
            method.Flags = MethodFlags.Public | MethodFlags.Static;
            var gen = method.GetGenerator();
            
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


            module.BakeDebugString();
            
            
            //File.WriteAllText(@"C:\Users\ls-mi\Desktop\wave.il", 
            //    module.BakeDebugString());
            
            var asm = new IshtarAssembly{Name = "woodo"};
            
            asm.AddSegment((".code", method.BakeByteArray()));
            
            //IshtarAssembly.WriteTo(asm, new DirectoryInfo(@"C:\Users\ls-mi\Desktop\"));

        }

        [Fact(Skip = "MANUAL")]
        public void TestIL()
        {
            var module = new WaveModuleBuilder("xuy");
            var clazz = module.DefineClass("global::wave/lang/svack_pidars");
            clazz.Flags = ClassFlags.Public | ClassFlags.Static;
            var method = clazz.DefineMethod("insert_dick_into_svack", MethodFlags.Public, WaveTypeCode.TYPE_VOID.AsClass(), ("x", WaveTypeCode.TYPE_STRING));
            method.Flags = MethodFlags.Public | MethodFlags.Static;
            var body = method.GetGenerator();
            
            body.Emit(OpCodes.LDC_I4_S, 1448);
            body.Emit(OpCodes.LDC_I4_S, 228);
            body.Emit(OpCodes.ADD);
            body.Emit(OpCodes.LDC_I4_S, 2);
            body.Emit(OpCodes.XOR);
            body.Emit(OpCodes.RESERVED_0);
            body.Emit(OpCodes.LDF, "x");
            body.Emit(OpCodes.RET);


            var body_module = module.BakeByteArray();


            var asm = new IshtarAssembly();
            
            asm.AddSegment((".code", body_module));
            
            //IshtarAssembly.WriteTo(asm, new DirectoryInfo(@"C:\Users\ls-mi\Desktop\"));
        }
        [Fact(Skip = "MANUAL")]
        public void AST2ILTest()
        {
            var w = new WaveSyntax();
            var ast = w.CompilationUnit.ParseWave(
                " class Program { main(): void { if(ze()) return x; else { return d();  } } }");

            var module = new WaveModuleBuilder("foo");

            foreach (var member in ast.Members)
            {
                if (member is ClassDeclarationSyntax classMember)
                {
                    var @class = module.DefineClass($"global::wave/lang/{classMember.Identifier}");

                    foreach (var methodMember in classMember.Methods)
                    {
                        var method = @class.DefineMethod(methodMember.Identifier, WaveTypeCode.TYPE_VOID.AsClass());
                        var generator = method.GetGenerator();

                        foreach (var statement in methodMember.Body.Statements)
                        {
                            var st = statement;
                        }
                    }
                }
            }
        }
        
        [Fact(Skip = "MANUAL")]
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
        
        [Fact(Skip = "MANUAL")]
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
        
        [Fact(Skip = "MANUAL")]
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
            var module = new WaveModuleBuilder(Guid.NewGuid().ToString());
            var @class = module.DefineClass("global::foo/bar");
            var method = @class.DefineMethod("foo", WaveTypeCode.TYPE_VOID.AsClass(), args);
            return method.GetGenerator();
        }
        [Fact(Skip = "MANUAL")]
        public void Fib()
        {
            /*let fib = fun (n) {
  if (n < 2) return n;
  return fib(n - 1) + fib(n - 2); 
}

let before = clock();
puts fib(40);
let after = clock();
puts after - before;*/
            long f(long n)
            {
                if (n == 0)
                {
                    return 0;
                }
                if (n == 1)
                {
                    return 1;
                }
                long first = 0;
                long second = 1;
                long nth = 1;
                for (long i = 2; i <= n; i++)
                {
                    nth = first + second;
                    first = second;
                    second = nth;
                }
                return nth;
            }
            
            var s = new Stopwatch();
            
            s.Start();
            //var a = f(int.MaxValue / 2);
            s.Stop();
            //_testOutputHelper.WriteLine($"{a}, {int.MaxValue / 2} {s.Elapsed.TotalMilliseconds / 1000f} seconds.");
        }
        [Fact(Skip = "MANUAL")]
        public void ManualGenCallExternFunction()
        {
            var module = new WaveModuleBuilder("hello_world");
            var clazz = module.DefineClass("hello_world%global::wave/lang/program");
            clazz.Flags = ClassFlags.Public | ClassFlags.Static;


            var f_println = clazz.DefineMethod("@_println", 
                MethodFlags.Extern, WaveTypeCode.TYPE_VOID.AsClass(),
                ("val", WaveTypeCode.TYPE_STRING));

            var f_readln = clazz.DefineMethod("@_readline", 
                MethodFlags.Extern, WaveTypeCode.TYPE_STRING.AsClass());


            var method = clazz.DefineMethod("master", MethodFlags.Public | MethodFlags.Static,
                WaveTypeCode.TYPE_VOID.AsClass());
            var body = method.GetGenerator();
            
            body.Emit(OpCodes.LDC_STR, $"\u001b[36mHello World\u001b[0m, from Wave Lang with Love {Emoji.Known.Sparkles}{Emoji.Known.Sparkles}{Emoji.Known.Sparkles}!");
            body.Emit(OpCodes.RESERVED_0);
            body.Emit(OpCodes.RESERVED_2);
            body.Emit(OpCodes.CALL, f_println);
            body.Emit(OpCodes.CALL, f_readln);
            body.Emit(OpCodes.CALL, f_println);
            body.Emit(OpCodes.RESERVED_2);
            body.Emit(OpCodes.RET);


            var body_module = module.BakeByteArray();


            var asm = new IshtarAssembly { Name = module.Name };
            
            asm.AddSegment((".code", body_module));
            
            IshtarAssembly.WriteTo(asm, new DirectoryInfo(@"C:\Users\ls-mi\Desktop\"));
            File.WriteAllText($@"C:\Users\ls-mi\Desktop\{module.Name}.wvil", module.BakeDebugString());
        }
        
        [Fact(Skip = "MANUAL")]
        public void ManualGen()
        {
            var module = new WaveModuleBuilder("satl");
            var clazz = module.DefineClass("satl%global::wave/lang/program");
            clazz.Flags = ClassFlags.Public | ClassFlags.Static;
            
            
            var fib = clazz.DefineMethod("fib", 
                MethodFlags.Public | MethodFlags.Static,
                WaveTypeCode.TYPE_I8.AsClass(), ("x", WaveTypeCode.TYPE_I8));

            var fibGen = fib.GetGenerator();

            //fibGen.Emit(OpCodes.LDC_I8_1);
            //fibGen.Emit(OpCodes.LDC_I8_1);
            //fibGen.Emit(OpCodes.EQL);
            //fibGen.Emit(OpCodes.RET);

            var label_if_1 = fibGen.DefineLabel();
            var label_if_2 = fibGen.DefineLabel();
            var for_1 = fibGen.DefineLabel();
            var for_body = fibGen.DefineLabel();

            // if (x == 0) return 0;
            fibGen.Emit(OpCodes.LDARG_0);
            fibGen.Emit(OpCodes.JMP_T, label_if_1);
            fibGen.Emit(OpCodes.LDC_I4_0);
            fibGen.Emit(OpCodes.RET);
            fibGen.UseLabel(label_if_1);
            // if (x == 1) return 1;
            fibGen.Emit(OpCodes.LDARG_0);
            fibGen.Emit(OpCodes.LDC_I4_1);
            fibGen.Emit(OpCodes.JMP_NN, label_if_2);
            fibGen.Emit(OpCodes.LDC_I4_1);
            fibGen.Emit(OpCodes.RET);
            fibGen.UseLabel(label_if_2);
            // var first, second, nth, i = 0;
            fibGen.Emit(OpCodes.LOC_INIT, new[]
            {
                WaveTypeCode.TYPE_I4, WaveTypeCode.TYPE_I4,
                WaveTypeCode.TYPE_I4, WaveTypeCode.TYPE_I4
            });
            // second, nth = 1; i = 2;
            fibGen.Emit(OpCodes.LDC_I4_1); fibGen.Emit(OpCodes.STLOC_1);
            fibGen.Emit(OpCodes.LDC_I4_1); fibGen.Emit(OpCodes.STLOC_2);
            fibGen.Emit(OpCodes.LDC_I4_2); fibGen.Emit(OpCodes.STLOC_3);

            // for
            // 
            fibGen.Emit(OpCodes.JMP, for_1);
            fibGen.UseLabel(for_body);
            fibGen.Emit(OpCodes.LDLOC_0);
            fibGen.Emit(OpCodes.LDLOC_1);
            fibGen.Emit(OpCodes.ADD);
            fibGen.Emit(OpCodes.STLOC_2);

            fibGen.Emit(OpCodes.LDLOC_1);
            fibGen.Emit(OpCodes.STLOC_0);

            fibGen.Emit(OpCodes.LDLOC_2);
            fibGen.Emit(OpCodes.STLOC_1);

            // i++
            fibGen.Emit(OpCodes.LDLOC_3);
            fibGen.Emit(OpCodes.LDC_I4_1);
            fibGen.Emit(OpCodes.ADD);
            fibGen.Emit(OpCodes.STLOC_3);

            // var exceptionType =
            //    module.FindType(new QualityTypeName("corlib%global::wave/lang/Exception")).AsClass();

            //fibGen.Emit(OpCodes.NEWOBJ, exceptionType.FullName);
            //fibGen.Emit(OpCodes.CALL, exceptionType.FindMethod("ctor()"));
            //fibGen.Emit(OpCodes.THROW);


            // i <= n
            fibGen.UseLabel(for_1);
            fibGen.Emit(OpCodes.NOP);
            fibGen.Emit(OpCodes.LDARG_0);
            fibGen.Emit(OpCodes.LDLOC_3);
            fibGen.Emit(OpCodes.JMP_LQ, for_body);
            // return nth;
            fibGen.Emit(OpCodes.LDLOC_2);
            fibGen.Emit(OpCodes.RET);

            var method = clazz.DefineMethod("master", MethodFlags.Public | MethodFlags.Static,
                WaveTypeCode.TYPE_VOID.AsClass());
            var body = method.GetGenerator();
            
            
            
            body.Emit(OpCodes.LDC_I4_S, 120/**/);
            body.Emit(OpCodes.CALL, fib);
            body.Emit(OpCodes.RESERVED_0);
            body.Emit(OpCodes.RESERVED_1);
            body.Emit(OpCodes.RET);
            //body.Emit(OpCodes.CALL);


            var body_module = module.BakeByteArray();


            var asm = new IshtarAssembly { Name = module.Name };
            
            asm.AddSegment((".code", body_module));
            
            IshtarAssembly.WriteTo(asm, new DirectoryInfo(@"C:\Users\ls-mi\Desktop\"));
            File.WriteAllText($@"C:\Users\ls-mi\Desktop\{module.Name}.wvil", module.BakeDebugString());
        }
    }
}