namespace wc_test
{
    using System;
    using System.Linq;
    using ishtar;
    using Sprache;
    using mana.ishtar.emit;
    using mana.runtime;
    using mana.stl;
    using mana.syntax;
    using NUnit.Framework;

    public class expression_test
    {
        public static ManaSyntax Syntax => new();


        [Test(Description = "(40 + 50)")]
        public void F00()
        {
            var f1 = ManaExpression.Const(ManaTypeCode.TYPE_I4, 40);
            var f2 = ManaExpression.Const(ManaTypeCode.TYPE_I4, 50);

            var result = ManaExpression.Add(f1, f2);

            Console.WriteLine(result.ToString());

            var type = result.DetermineType(null);

            Assert.AreEqual(ManaTypeCode.TYPE_I4, type.TypeCode);
        }
        [Test(Description = "((40 + 50) - 50)")]
        public void F01()
        {
            var f1 = ManaExpression.Const(ManaTypeCode.TYPE_I4, 40);
            var f2 = ManaExpression.Const(ManaTypeCode.TYPE_I4, 50);

            var f3 = ManaExpression.Add(f1, f2);

            var result = ManaExpression.Sub(f3, f2);

            Console.WriteLine(result.ToString());

            var type = result.DetermineType(null);

            Console.WriteLine($"result: {result.ForceOptimization().ExpressionString}");

            Assert.AreEqual(ManaTypeCode.TYPE_I4, type.TypeCode);
        }
        [Test(Description = "(((40 - (40 + 50)) / (40 + 50)) - ((40 - (40 + 50)) / (40 + 50)))")]
        public void F02()
        {
            var f1 = ManaExpression.Const(ManaTypeCode.TYPE_I4, 40);
            var f2 = ManaExpression.Const(ManaTypeCode.TYPE_I4, 50);

            var f3 = ManaExpression.Add(f1, f2);
            var f4 = ManaExpression.Sub(f1, f3);
            var f5 = ManaExpression.Div(f4, f3);

            var result = ManaExpression.Sub(f5, f5);

            Console.WriteLine(result.ToString());

            var type = result.DetermineType(null);

            Assert.AreEqual(ManaTypeCode.TYPE_I4, type.TypeCode);
        }

        [Test(Description = "(((40 - (40 + 50)) / (40 + 50)) && ((40 - (40 + 50)) / (40 + 50)))")]
        public void F03()
        {
            var f1 = ManaExpression.Const(ManaTypeCode.TYPE_I4, 40);
            var f2 = ManaExpression.Const(ManaTypeCode.TYPE_I4, 50);

            var f3 = ManaExpression.Add(f1, f2);
            var f4 = ManaExpression.Sub(f1, f3);
            var f5 = ManaExpression.Div(f4, f3);

            var result = ManaExpression.AndAlso(f5, f5);

            Console.WriteLine(result.ToString());

            var type = result.DetermineType(null);

            Assert.AreEqual(ManaTypeCode.TYPE_BOOLEAN, type.TypeCode);
        }

        [Test]
        public void F04()
        {
            var f1 = ManaExpression.Const(ManaTypeCode.TYPE_I8, 40);
            var f2 = ManaExpression.Const(ManaTypeCode.TYPE_I4, 50);

            var result = ManaExpression.AndAlso(f1, f2);

            Console.WriteLine(result.ToString());

            var type = result.DetermineType(null);

            Assert.AreEqual(ManaTypeCode.TYPE_BOOLEAN, type.TypeCode);
        }

        [Test]
        public void F05()
        {
            var f1 = ManaExpression.Const(ManaTypeCode.TYPE_I8, long.MaxValue - 200);
            var f2 = ManaExpression.Const(ManaTypeCode.TYPE_I4, 50);

            var result = ManaExpression.Sub(f1, f2);

            Console.WriteLine(result.ToString());

            var type = result.DetermineType(null);

            Assert.AreEqual(ManaTypeCode.TYPE_I8, type.TypeCode);
        }

        [Test]
        public void F06()
        {
            var f1 = ManaExpression.Const(ManaTypeCode.TYPE_STRING, "Foo");
            var f2 = ManaExpression.Const(ManaTypeCode.TYPE_I4, 50);

            var result = ManaExpression.Sub(f1, f2);

            Console.WriteLine(result.ToString());


            Assert.Throws<Exception>(() => result.DetermineType(null));
        }


        [Test, Ignore("TODO")]
        public void DetermineVariableType()
        {
            var genCtx = new GeneratorContext();

            genCtx.Module = new ManaModuleBuilder("doo");
            var @class = genCtx.Module.DefineClass("global::mana/foo");
            genCtx.CurrentMethod = @class.DefineMethod("ata", MethodFlags.Public, ManaTypeCode.TYPE_VOID.AsClass());
            genCtx.CurrentScope = new ManaScope(genCtx);

            genCtx.CurrentScope.DefineVariable(new IdentifierExpression("idi"), ManaTypeCode.TYPE_BOOLEAN.AsClass(), 0);

            var key = $"idi";
            var id = Syntax.QualifiedExpression.End().ParseMana(key) as IdentifierExpression;
            var result = new MemberAccessExpression(id, Array.Empty<ExpressionSyntax>(), Array.Empty<ExpressionSyntax>());

            var chain = result.GetChain().ToArray();

            IshtarAssert.NotEmpty(chain);

            var type = result.DetermineType(genCtx);

            Assert.AreEqual(ManaTypeCode.TYPE_BOOLEAN, type.TypeCode);
        }

        [Test]
        public void DetermineSelfMethodType()
        {
            var genCtx = new GeneratorContext();

            genCtx.Module = new ManaModuleBuilder("doo");
            var @class = genCtx.Module.DefineClass("global::mana/foo");
            genCtx.CurrentMethod = @class.DefineMethod("ata", MethodFlags.Public, ManaTypeCode.TYPE_VOID.AsClass());
            genCtx.CurrentScope = new ManaScope(genCtx);

            var key = $"ata()";
            var result = Syntax.QualifiedExpression.End().ParseMana(key) as MemberAccessExpression;

            Assert.NotNull(result);

            var chain = result.GetChain().ToArray();

            IshtarAssert.NotEmpty(chain);

            var type = result.DetermineType(genCtx);

            Assert.IsEmpty(genCtx.Errors);

            Assert.AreEqual(ManaTypeCode.TYPE_VOID, type.TypeCode);
        }

        [Test, Ignore("Bug in CI, System.InvalidOperationException : There is no currently active test.")]
        public void DetermineOtherMethodType()
        {
            var genCtx = new GeneratorContext();

            genCtx.Module = new ManaModuleBuilder("doo");
            var @class = genCtx.Module.DefineClass("global::mana/foo");
            var anotherClass = genCtx.Module.DefineClass("global::mana/goo");

            anotherClass.DefineMethod("gota", MethodFlags.Public, ManaTypeCode.TYPE_I1.AsClass());

            @class.Includes.Add("global::mana");

            genCtx.CurrentMethod = @class.DefineMethod("ata", MethodFlags.Public, ManaTypeCode.TYPE_VOID.AsClass());
            genCtx.CurrentScope = new ManaScope(genCtx);

            genCtx.CurrentScope.DefineVariable(new IdentifierExpression("ow"), anotherClass, 0);

            var result = Syntax.QualifiedExpression
                    .End()
                    .ParseMana("ow.gota()")
                as MemberAccessExpression;

            Assert.NotNull(result);

            var chain = result.GetChain().ToArray();

            IshtarAssert.NotEmpty(chain);

            var type = result.DetermineType(genCtx);


            Assert.IsEmpty(genCtx.Errors);

            Assert.AreEqual(ManaTypeCode.TYPE_I1, type.TypeCode);

        }
    }
}
