namespace veinc_test
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using ishtar;
    using Spectre.Console;
    using vein.fs;
    using ishtar.emit;
    using vein.runtime;
    using vein.stl;
    using vein.syntax;
    using NUnit.Framework;

//    public class generator_test
//    {
//        [Test, Ignore("MANUAL")]
//        public void Test()
//        {
//            var module = new VeinModuleBuilder("xuy", (Types.Storage));
//            var clazz = module.DefineClass("xuy%vein/lang/svack_pidars");
//            clazz.Flags = ClassFlags.Public | ClassFlags.Static;
//            var method = clazz.DefineMethod("insert_dick_into_svack", MethodFlags.Public, VeinTypeCode.TYPE_VOID.AsClass()((Types.Storage)), VeinArgumentRef.Create(Types.Storage, ("x", VeinTypeCode.TYPE_STRING)));
//            method.Flags = MethodFlags.Public | MethodFlags.Static;
//            var gen = method.GetGenerator();

//            var l1 = gen.DefineLabel("start_loop");
//            var l2 = gen.DefineLabel("end_loop");
//            gen.Emit(OpCodes.ADD);
//            gen.Emit(OpCodes.LDC_I4_S, 228);
//            gen.Emit(OpCodes.ADD);
//            gen.Emit(OpCodes.JMP_HQ, l2);
//            gen.Emit(OpCodes.LDC_I4_S, 228);
//            gen.Emit(OpCodes.ADD);
//            gen.Emit(OpCodes.LDC_I4_S, 228);
//            gen.Emit(OpCodes.LDC_I4_S, 228);
//            gen.Emit(OpCodes.ADD);
//            gen.Emit(OpCodes.JMP_HQ, l1);
//            gen.UseLabel(l1);
//            gen.Emit(OpCodes.SUB);
//            gen.Emit(OpCodes.SUB);
//            gen.UseLabel(l2);
//            gen.Emit(OpCodes.SUB);
//            gen.Emit(OpCodes.SUB);


//            module.BakeDebugString();


//            //File.WriteAllText(@"C:\Users\ls-mi\Desktop\mana.il", 
//            //    module.BakeDebugString());

//            var asm = new IshtarAssembly{Name = "woodo"};

//            asm.AddSegment((".code", method.BakeByteArray()));

//            //IshtarAssembly.WriteTo(asm, new DirectoryInfo(@"C:\Users\ls-mi\Desktop\"));

//        }

//        [Test, Ignore("MANUAL")]
//        public void TestIL()
//        {
//            var module = new VeinModuleBuilder("xuy", (Types.Storage));
//            var clazz = module.DefineClass("vein/lang/svack_pidars");
//            clazz.Flags = ClassFlags.Public | ClassFlags.Static;
//            var method = clazz.DefineMethod("insert_dick_into_svack", MethodFlags.Public, VeinTypeCode.TYPE_VOID.AsClass()((Types.Storage)), VeinArgumentRef.Create(Types.Storage, ("x", VeinTypeCode.TYPE_STRING)));
//            method.Flags = MethodFlags.Public | MethodFlags.Static;
//            var body = method.GetGenerator();

//            body.Emit(OpCodes.LDC_I4_S, 1448);
//            body.Emit(OpCodes.LDC_I4_S, 228);
//            body.Emit(OpCodes.ADD);
//            body.Emit(OpCodes.LDC_I4_S, 2);
//            body.Emit(OpCodes.XOR);
//            body.Emit(OpCodes.RESERVED_0);
//            body.Emit(OpCodes.LDF, "x");
//            body.Emit(OpCodes.RET);


//            var body_module = module.BakeByteArray();


//            var asm = new IshtarAssembly();

//            asm.AddSegment((".code", body_module));

//            //IshtarAssembly.WriteTo(asm, new DirectoryInfo(@"C:\Users\ls-mi\Desktop\"));
//        }
//        [Test, Ignore("MANUAL")]
//        public void AST2ILTest()
//        {
//            var w = new VeinSyntax();
//            var ast = w.CompilationUnit.ParseVein(
//                " class Program { main(): void { if(ze()) return x; else { return d();  } } }");

//            var module = new VeinModuleBuilder("foo", (Types.Storage));

//            foreach (var member in ast.Members)
//            {
//                if (member is ClassDeclarationSyntax classMember)
//                {
//                    var @class = module.DefineClass($"vein/lang/{classMember.Identifier}");

//                    foreach (var methodMember in classMember.Methods)
//                    {
//                        var method = @class.DefineMethod(methodMember.Identifier.ExpressionString, VeinTypeCode.TYPE_VOID.AsClass()(Types.Storage));
//                        var generator = method.GetGenerator();

//                        foreach (var statement in methodMember.Body.Statements)
//                        {
//                            var st = statement;
//                        }
//                    }
//                }
//            }
//        }

//        [Test]
//        public void ReturnStatementCompilation1()
//        {
//            var ret = new ReturnStatementSyntax(new SingleLiteralExpressionSyntax(14.3f));

//            var actual = CreateGenerator();

//            actual.EmitReturn(ret);

//            var expected = CreateGenerator();

//            expected.Emit(OpCodes.LDC_F4, 14.3f);
//            expected.Emit(OpCodes.RET);


//            Assert.AreEqual(expected.BakeByteArray(), actual.BakeByteArray());
//        }

//        [Test]
//        public void ReturnStatementCompilation2()
//        {
//            var ret = new ReturnStatementSyntax(new IdentifierExpression("x"));

//            var actual = CreateGenerator();
//            var ctx_actual = actual.ConsumeFromMetadata<GeneratorContext>("context");

//            ctx_actual.Classes.First().Value
//                .DefineField("x", FieldFlags.None, VeinTypeCode.TYPE_STRING.AsClass()(Types.Storage));


//            actual.EmitReturn(ret);

//            var expected = CreateGenerator();
//            var ctx_expected = expected.ConsumeFromMetadata<GeneratorContext>("context");

//            var field = ctx_expected.Classes.First().Value
//                .DefineField("x", FieldFlags.None, VeinTypeCode.TYPE_STRING.AsClass()(Types.Storage));

//            expected.Emit(OpCodes.LDARG_0);
//            expected.Emit(OpCodes.LDF, field);
//            expected.Emit(OpCodes.RET);

//            IshtarAssert.SequenceEqual(expected._debug_list, actual._debug_list);
//        }
//        [Test]
//        public void ReturnStatementCompilation3()
//        {
//            var ret = new ReturnStatementSyntax(new IdentifierExpression("x"));

//            var actual = CreateGenerator(VeinArgumentRef.Create(Types.Storage, ("x", VeinTypeCode.TYPE_STRING)));

//            actual.EmitReturn(ret);

//            var expected = CreateGenerator(VeinArgumentRef.Create(Types.Storage, ("x", VeinTypeCode.TYPE_STRING)));

//            expected.Emit(OpCodes.LDARG_0);
//            expected.Emit(OpCodes.RET);

//            IshtarAssert.SequenceEqual(expected._debug_list, actual._debug_list);
//        }

//        public static ILGenerator CreateGenerator(params VeinArgumentRef[] args)
//        {
//            var module = new VeinModuleBuilder(Guid.NewGuid().ToString(), (Types.Storage));
//            var @class = module.DefineClass("foo/bar");
//            var method = @class.DefineMethod("foo", VeinTypeCode.TYPE_VOID.AsClass()(Types.Storage), args);

//            var gen =  method.GetGenerator();
//            var ctx = new GeneratorContext(new GeneratorContextConfig(true));
//            ctx.Module = module;
//            ctx.Classes.Add(@class.FullName, @class);
//            ctx.CurrentMethod = method;
//            gen.StoreIntoMetadata("context", ctx);
//            ctx.CreateScope();
//            return gen;
//        }
//        [Test, Ignore("MANUAL")]
//        public void Fib()
//        {
//            /*let fib = fun (n) {
//  if (n < 2) return n;
//  return fib(n - 1) + fib(n - 2); 
//}

//let before = clock();
//puts fib(40);
//let after = clock();
//puts after - before;*/
//            //long f(long n)
//            //{
//            //    if (n == 0)
//            //    {
//            //        return 0;
//            //    }
//            //    if (n == 1)
//            //    {
//            //        return 1;
//            //    }
//            //    long first = 0;
//            //    long second = 1;
//            //    long nth = 1;
//            //    for (long i = 2; i <= n; i++)
//            //    {
//            //        nth = first + second;
//            //        first = second;
//            //        second = nth;
//            //    }
//            //    return nth;
//            //}

//            var s = new Stopwatch();

//            s.Start();
//            //var a = f(int.MaxValue / 2);
//            s.Stop();
//            //Console.WriteLine($"{a}, {int.MaxValue / 2} {s.Elapsed.TotalMilliseconds / 1000f} seconds.");
//        }
//        [Test, Ignore("MANUAL")]
//        public void ManualGenCallExternFunction()
//        {
//            var module = new VeinModuleBuilder("hello_world", (Types.Storage));
//            var clazz = module.DefineClass("hello_world%wave/lang/program");
//            clazz.Flags = ClassFlags.Public | ClassFlags.Static;


//            var method = clazz.DefineMethod("master", MethodFlags.Public | MethodFlags.Static,
//                VeinTypeCode.TYPE_VOID.AsClass()(Types.Storage));
//            var body = method.GetGenerator();

//            var @while = body.DefineLabel("while");

//            body.UseLabel(@while);
//            body.Emit(OpCodes.NOP);
//            body.Emit(OpCodes.NEWOBJ, (Types.Storage).StringClass);
//            body.Emit(OpCodes.RESERVED_2);
//            body.Emit(OpCodes.JMP, @while);
//            body.Emit(OpCodes.RET);


//            var body_module = module.BakeByteArray();


//            var asm = new IshtarAssembly { Name = module.Name.moduleName };

//            asm.AddSegment((".code", body_module));

//            IshtarAssembly.WriteTo(asm, new DirectoryInfo(@"C:\Users\ls-mi\Desktop\"));
//            File.WriteAllText($@"C:\Users\ls-mi\Desktop\{module.Name}.wvil", module.BakeDebugString());
//        }

//        [Test, Ignore("MANUAL")]
//        public void ManualGen()
//        {
//            var module = new VeinModuleBuilder("satl", (Types.Storage));
//            var clazz = module.DefineClass("satl%wave/lang/program");
//            clazz.Flags = ClassFlags.Public | ClassFlags.Static;


//            var fib = clazz.DefineMethod("fib",
//                MethodFlags.Public | MethodFlags.Static,
//                VeinTypeCode.TYPE_I8.AsClass()((Types.Storage)), VeinArgumentRef.Create(Types.Storage, ("x", VeinTypeCode.TYPE_I8)));

//            var fibGen = fib.GetGenerator();

//            //fibGen.Emit(OpCodes.LDC_I8_1);
//            //fibGen.Emit(OpCodes.LDC_I8_1);
//            //fibGen.Emit(OpCodes.EQL);
//            //fibGen.Emit(OpCodes.RET);

//            var label_if_1 = fibGen.DefineLabel("check_zero");
//            var label_if_2 = fibGen.DefineLabel("check_one");
//            var for_1 = fibGen.DefineLabel("loop_start");
//            var for_body = fibGen.DefineLabel("loop_body");

//            // if (x == 0) return 0;
//            fibGen.Emit(OpCodes.LDARG_1);
//            fibGen.Emit(OpCodes.JMP_T, label_if_1);
//            fibGen.Emit(OpCodes.LDC_I4_0);
//            fibGen.Emit(OpCodes.RET);
//            fibGen.UseLabel(label_if_1);
//            // if (x == 1) return 1;
//            fibGen.Emit(OpCodes.LDARG_1);
//            fibGen.Emit(OpCodes.LDC_I4_1);
//            fibGen.Emit(OpCodes.JMP_NN, label_if_2);
//            fibGen.Emit(OpCodes.LDC_I4_1);
//            fibGen.Emit(OpCodes.RET);
//            fibGen.UseLabel(label_if_2);
//            // var first, second, nth, i = 0;
//            fibGen.EnsureLocal("first", VeinTypeCode.TYPE_I4.AsClass()(Types.Storage));
//            fibGen.EnsureLocal("second", VeinTypeCode.TYPE_I4.AsClass()(Types.Storage));
//            fibGen.EnsureLocal("nth", VeinTypeCode.TYPE_I4.AsClass()(Types.Storage));
//            fibGen.EnsureLocal("i", VeinTypeCode.TYPE_I4.AsClass()(Types.Storage));
//            // second, nth = 1; i = 2;
//            fibGen.Emit(OpCodes.LDC_I4_1); fibGen.Emit(OpCodes.STLOC_1);
//            fibGen.Emit(OpCodes.LDC_I4_1); fibGen.Emit(OpCodes.STLOC_2);
//            fibGen.Emit(OpCodes.LDC_I4_2); fibGen.Emit(OpCodes.STLOC_3);

//            // for
//            // 
//            fibGen.Emit(OpCodes.JMP, for_1);
//            fibGen.UseLabel(for_body);
//            fibGen.Emit(OpCodes.LDLOC_0);
//            fibGen.Emit(OpCodes.LDLOC_1);
//            fibGen.Emit(OpCodes.ADD);
//            fibGen.Emit(OpCodes.STLOC_2);

//            fibGen.Emit(OpCodes.LDLOC_1);
//            fibGen.Emit(OpCodes.STLOC_0);

//            fibGen.Emit(OpCodes.LDLOC_2);
//            fibGen.Emit(OpCodes.STLOC_1);

//            // i++
//            fibGen.Emit(OpCodes.LDLOC_3);
//            fibGen.Emit(OpCodes.LDC_I4_1);
//            fibGen.Emit(OpCodes.ADD);
//            fibGen.Emit(OpCodes.STLOC_3);

//            // var exceptionType =
//            //    module.FindType(new QualityTypeName("std%wave/lang/Exception")).AsClass();

//            //fibGen.Emit(OpCodes.NEWOBJ, exceptionType.FullName);
//            //fibGen.Emit(OpCodes.CALL, exceptionType.FindMethod("ctor()"));
//            //fibGen.Emit(OpCodes.THROW);


//            // i <= n
//            fibGen.UseLabel(for_1);
//            fibGen.Emit(OpCodes.NOP);
//            fibGen.Emit(OpCodes.LDARG_1);
//            fibGen.Emit(OpCodes.LDLOC_3);
//            fibGen.Emit(OpCodes.JMP_LQ, for_body);
//            // return nth;
//            fibGen.Emit(OpCodes.LDLOC_2);
//            fibGen.Emit(OpCodes.RET);

//            var method = clazz.DefineMethod("master", MethodFlags.Public | MethodFlags.Static,
//                VeinTypeCode.TYPE_VOID.AsClass()(Types.Storage));
//            var body = method.GetGenerator();



//            body.Emit(OpCodes.LDC_I4_S, 120/**/);
//            body.Emit(OpCodes.CALL, fib);
//            body.Emit(OpCodes.RESERVED_0);
//            body.Emit(OpCodes.RESERVED_1);
//            body.Emit(OpCodes.RET);
//            //body.Emit(OpCodes.CALL);


//            var body_module = module.BakeByteArray();


//            var asm = new IshtarAssembly { Name = module.Name };

//            asm.AddSegment((".code", body_module));

//            IshtarAssembly.WriteTo(asm, new DirectoryInfo(@"C:\Users\ls-mi\Desktop\"));
//            File.WriteAllText($@"C:\Users\ls-mi\Desktop\{module.Name}.wvil", module.BakeDebugString());
//        }
//    }
}
