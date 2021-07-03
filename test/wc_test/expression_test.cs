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
    using Xunit;
    using Xunit.Abstractions;

    public class expression_test
    {
        private readonly ITestOutputHelper _testOutputHelper;
        public static ManaSyntax Sytnax => new();

        public expression_test(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        [Fact(DisplayName = "(40 + 50)")]
        public void F00()
        {
            var f1 = ManaExpression.Const(ManaTypeCode.TYPE_I4, 40);
            var f2 = ManaExpression.Const(ManaTypeCode.TYPE_I4, 50);

            var result = ManaExpression.Add(f1, f2);

            _testOutputHelper.WriteLine(result.ToString());

            var type = result.DetermineType(null);

            Assert.Equal(ManaTypeCode.TYPE_I4, type.TypeCode);
        }
        [Fact(DisplayName = "((40 + 50) - 50)")]
        public void F01()
        {
            var f1 = ManaExpression.Const(ManaTypeCode.TYPE_I4, 40);
            var f2 = ManaExpression.Const(ManaTypeCode.TYPE_I4, 50);

            var f3 = ManaExpression.Add(f1, f2);

            var result = ManaExpression.Sub(f3, f2);

            _testOutputHelper.WriteLine(result.ToString());

            var type = result.DetermineType(null);

            _testOutputHelper.WriteLine($"result: {result.ForceOptimization().ExpressionString}");

            Assert.Equal(ManaTypeCode.TYPE_I4, type.TypeCode);
        }
        [Fact(DisplayName = "(((40 - (40 + 50)) / (40 + 50)) - ((40 - (40 + 50)) / (40 + 50)))")]
        public void F02()
        {
            var f1 = ManaExpression.Const(ManaTypeCode.TYPE_I4, 40);
            var f2 = ManaExpression.Const(ManaTypeCode.TYPE_I4, 50);

            var f3 = ManaExpression.Add(f1, f2);
            var f4 = ManaExpression.Sub(f1, f3);
            var f5 = ManaExpression.Div(f4, f3);

            var result = ManaExpression.Sub(f5, f5);

            _testOutputHelper.WriteLine(result.ToString());

            var type = result.DetermineType(null);

            Assert.Equal(ManaTypeCode.TYPE_I4, type.TypeCode);
        }

        [Fact(DisplayName = "(((40 - (40 + 50)) / (40 + 50)) && ((40 - (40 + 50)) / (40 + 50)))")]
        public void F03()
        {
            var f1 = ManaExpression.Const(ManaTypeCode.TYPE_I4, 40);
            var f2 = ManaExpression.Const(ManaTypeCode.TYPE_I4, 50);

            var f3 = ManaExpression.Add(f1, f2);
            var f4 = ManaExpression.Sub(f1, f3);
            var f5 = ManaExpression.Div(f4, f3);

            var result = ManaExpression.AndAlso(f5, f5);

            _testOutputHelper.WriteLine(result.ToString());

            var type = result.DetermineType(null);

            Assert.Equal(ManaTypeCode.TYPE_BOOLEAN, type.TypeCode);
        }

        [Fact]
        public void F04()
        {
            var f1 = ManaExpression.Const(ManaTypeCode.TYPE_I8, 40);
            var f2 = ManaExpression.Const(ManaTypeCode.TYPE_I4, 50);

            var result = ManaExpression.AndAlso(f1, f2);

            _testOutputHelper.WriteLine(result.ToString());

            var type = result.DetermineType(null);

            Assert.Equal(ManaTypeCode.TYPE_BOOLEAN, type.TypeCode);
        }

        [Fact]
        public void F05()
        {
            var f1 = ManaExpression.Const(ManaTypeCode.TYPE_I8, long.MaxValue - 200);
            var f2 = ManaExpression.Const(ManaTypeCode.TYPE_I4, 50);

            var result = ManaExpression.Sub(f1, f2);

            _testOutputHelper.WriteLine(result.ToString());

            var type = result.DetermineType(null);

            Assert.Equal(ManaTypeCode.TYPE_I8, type.TypeCode);
        }

        [Fact]
        public void F06()
        {
            var f1 = ManaExpression.Const(ManaTypeCode.TYPE_STRING, "Foo");
            var f2 = ManaExpression.Const(ManaTypeCode.TYPE_I4, 50);

            var result = ManaExpression.Sub(f1, f2);

            _testOutputHelper.WriteLine(result.ToString());


            Assert.Throws<Exception>(() => result.DetermineType(null));
        }


        [Fact(Skip = "TODO")]
        public void DetermineVariableType()
        {
            var genCtx = new GeneratorContext();

            genCtx.Module = new ManaModuleBuilder("doo");
            var @class = genCtx.Module.DefineClass("global::mana/foo");
            genCtx.CurrentMethod = @class.DefineMethod("ata", MethodFlags.Public, ManaTypeCode.TYPE_VOID.AsClass());
            genCtx.CurrentScope = new ManaScope(genCtx);

            genCtx.CurrentScope.DefineVariable(new IdentifierExpression("idi"), ManaTypeCode.TYPE_BOOLEAN.AsClass(), 0);

            var key = $"idi";
            var id = Sytnax.QualifiedExpression.End().ParseMana(key) as IdentifierExpression;
            var result = new MemberAccessExpression(id, Array.Empty<ExpressionSyntax>(), Array.Empty<ExpressionSyntax>());

            var chain = result.GetChain().ToArray();

            Assert.NotEmpty(chain);

            var type = result.DetermineType(genCtx);

            Assert.Equal(ManaTypeCode.TYPE_BOOLEAN, type.TypeCode);
        }

        [Fact]
        public void DetermineSelfMethodType()
        {
            var genCtx = new GeneratorContext();

            genCtx.Module = new ManaModuleBuilder("doo");
            var @class = genCtx.Module.DefineClass("global::mana/foo");
            genCtx.CurrentMethod = @class.DefineMethod("ata", MethodFlags.Public, ManaTypeCode.TYPE_VOID.AsClass());
            genCtx.CurrentScope = new ManaScope(genCtx);

            var key = $"ata()";
            var result = Sytnax.QualifiedExpression.End().ParseMana(key) as MemberAccessExpression;

            Assert.NotNull(result);

            var chain = result.GetChain().ToArray();

            Assert.NotEmpty(chain);

            var type = result.DetermineType(genCtx);

            Assert.Empty(genCtx.Errors);

            Assert.Equal(ManaTypeCode.TYPE_VOID, type.TypeCode);
        }

        [Fact(Skip = "Bug in CI, System.InvalidOperationException : There is no currently active test.")]
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

            var result = Sytnax.QualifiedExpression
                    .End()
                    .ParseMana("ow.gota()")
                as MemberAccessExpression;

            Assert.NotNull(result);

            var chain = result.GetChain().ToArray();

            Assert.NotEmpty(chain);

            var type = result.DetermineType(genCtx);


            Assert.Empty(genCtx.Errors);

            Assert.Equal(ManaTypeCode.TYPE_I1, type.TypeCode);

        }
    }
}
