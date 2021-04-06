namespace wc_test
{
    using System;
    using System.Linq;
    using insomnia.emit;
    using insomnia.extensions;
    using insomnia.stl;
    using insomnia.syntax;
    using Sprache;
    using wave.etc;
    using Xunit;
    using Xunit.Abstractions;

    public class expression_test
    {
        private readonly ITestOutputHelper _testOutputHelper;
        public static WaveSyntax Wave => new();

        public expression_test(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        [Fact(DisplayName = "(40 + 50)")]
        public void F00()
        {
            var f1 = WaveExpression.Const(WaveTypeCode.TYPE_I4, 40);
            var f2 = WaveExpression.Const(WaveTypeCode.TYPE_I4, 50);

            var result = WaveExpression.Add(f1, f2);

            _testOutputHelper.WriteLine(result.ToString());

            var type = result.DetermineType(null);

            Assert.Equal(WaveTypeCode.TYPE_I4, type.TypeCode);
        }
        [Fact(DisplayName = "((40 + 50) - 50)")]
        public void F01()
        {
            var f1 = WaveExpression.Const(WaveTypeCode.TYPE_I4, 40);
            var f2 = WaveExpression.Const(WaveTypeCode.TYPE_I4, 50);

            var f3 = WaveExpression.Add(f1, f2);

            var result = WaveExpression.Sub(f3, f2);

            _testOutputHelper.WriteLine(result.ToString());

            var type = result.DetermineType(null);

            _testOutputHelper.WriteLine($"result: {result.ForceOptimization().ExpressionString}");

            Assert.Equal(WaveTypeCode.TYPE_I4, type.TypeCode);
        }
        [Fact(DisplayName = "(((40 - (40 + 50)) / (40 + 50)) - ((40 - (40 + 50)) / (40 + 50)))")]
        public void F02()
        {
            var f1 = WaveExpression.Const(WaveTypeCode.TYPE_I4, 40);
            var f2 = WaveExpression.Const(WaveTypeCode.TYPE_I4, 50);

            var f3 = WaveExpression.Add(f1, f2);
            var f4 = WaveExpression.Sub(f1, f3);
            var f5 = WaveExpression.Div(f4, f3);

            var result = WaveExpression.Sub(f5, f5);

            _testOutputHelper.WriteLine(result.ToString());

            var type = result.DetermineType(null);

            Assert.Equal(WaveTypeCode.TYPE_I4, type.TypeCode);
        }

        [Fact(DisplayName = "(((40 - (40 + 50)) / (40 + 50)) && ((40 - (40 + 50)) / (40 + 50)))")]
        public void F03()
        {
            var f1 = WaveExpression.Const(WaveTypeCode.TYPE_I4, 40);
            var f2 = WaveExpression.Const(WaveTypeCode.TYPE_I4, 50);

            var f3 = WaveExpression.Add(f1, f2);
            var f4 = WaveExpression.Sub(f1, f3);
            var f5 = WaveExpression.Div(f4, f3);

            var result = WaveExpression.AndAlso(f5, f5);

            _testOutputHelper.WriteLine(result.ToString());

            var type = result.DetermineType(null);

            Assert.Equal(WaveTypeCode.TYPE_BOOLEAN, type.TypeCode);
        }

        [Fact]
        public void F04()
        {
            var f1 = WaveExpression.Const(WaveTypeCode.TYPE_I8, 40);
            var f2 = WaveExpression.Const(WaveTypeCode.TYPE_I4, 50);
            
            var result = WaveExpression.AndAlso(f1, f2);

            _testOutputHelper.WriteLine(result.ToString());

            var type = result.DetermineType(null);

            Assert.Equal(WaveTypeCode.TYPE_BOOLEAN, type.TypeCode);
        }

        [Fact]
        public void F05()
        {
            var f1 = WaveExpression.Const(WaveTypeCode.TYPE_I8, long.MaxValue - 200);
            var f2 = WaveExpression.Const(WaveTypeCode.TYPE_I4, 50);
            
            var result = WaveExpression.Sub(f1, f2);

            _testOutputHelper.WriteLine(result.ToString());

            var type = result.DetermineType(null);

            Assert.Equal(WaveTypeCode.TYPE_I8, type.TypeCode);
        }

        [Fact]
        public void F06()
        {
            var f1 = WaveExpression.Const(WaveTypeCode.TYPE_STRING, "Foo");
            var f2 = WaveExpression.Const(WaveTypeCode.TYPE_I4, 50);
            
            var result = WaveExpression.Sub(f1, f2);

            _testOutputHelper.WriteLine(result.ToString());


            Assert.Throws<NotImplementedException>(() => result.DetermineType(null));
        }


        [Fact]
        public void DetermineVariableType()
        {
            var genCtx = new GeneratorContext();

            genCtx.Module = new WaveModuleBuilder("doo");
            var @class = genCtx.Module.DefineClass("global::wave/foo");
            genCtx.CurrentMethod = @class.DefineMethod("ata", MethodFlags.Public, WaveTypeCode.TYPE_VOID.AsType());
            genCtx.CurrentScope = new WaveScope(genCtx);

            genCtx.CurrentScope.DefineVariable(new IdentifierExpression("idi"), WaveTypeCode.TYPE_BOOLEAN.AsType());

            var key = $"idi";
            var id = Wave.QualifiedExpression.End().ParseWave(key) as IdentifierExpression;
            var result = new MemberAccessExpression(id, Array.Empty<ExpressionSyntax>(), Array.Empty<ExpressionSyntax>());
            
            var chain = result.GetChain().ToArray();

            Assert.NotEmpty(chain);

            var type = result.DetermineType(genCtx);

            Assert.Equal(WaveTypeCode.TYPE_BOOLEAN, type.TypeCode);
        }

        [Fact]
        public void DetermineSelfMethodType()
        {
            var genCtx = new GeneratorContext();

            genCtx.Module = new WaveModuleBuilder("doo");
            var @class = genCtx.Module.DefineClass("global::wave/foo");
            genCtx.CurrentMethod = @class.DefineMethod("ata", MethodFlags.Public, WaveTypeCode.TYPE_VOID.AsType());
            genCtx.CurrentScope = new WaveScope(genCtx);
            
            var key = $"ata()";
            var result = Wave.QualifiedExpression.End().ParseWave(key) as MemberAccessExpression;

            Assert.NotNull(result);

            var chain = result.GetChain().ToArray();

            Assert.NotEmpty(chain);

            var type = result.DetermineType(genCtx);

            Assert.Empty(genCtx.Errors);

            Assert.Equal(WaveTypeCode.TYPE_VOID, type.TypeCode);
        }

        [Fact]
        public void DetermineOtherMethodType()
        {
            var genCtx = new GeneratorContext();

            genCtx.Module = new WaveModuleBuilder("doo");
            var @class = genCtx.Module.DefineClass("global::wave/foo");
            var anotherClass = genCtx.Module.DefineClass("global::wave/goo");

            anotherClass.DefineMethod("gota", MethodFlags.Public, WaveTypeCode.TYPE_I1.AsType());

            @class.Includes.Add("global::wave");

            genCtx.CurrentMethod = @class.DefineMethod("ata", MethodFlags.Public, WaveTypeCode.TYPE_VOID.AsType());
            genCtx.CurrentScope = new WaveScope(genCtx);

            genCtx.CurrentScope.DefineVariable(new IdentifierExpression("ow"), anotherClass.AsType());
            
            var result = Wave.QualifiedExpression
                    .End()
                    .ParseWave("ow.gota()") 
                as MemberAccessExpression;

            Assert.NotNull(result);

            var chain = result.GetChain().ToArray();

            Assert.NotEmpty(chain);

            var type = result.DetermineType(genCtx);


            Assert.Empty(genCtx.Errors);

            Assert.Equal(WaveTypeCode.TYPE_I1, type.TypeCode);
            
        }
    }
}