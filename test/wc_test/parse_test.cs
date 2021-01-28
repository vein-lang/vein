namespace wc_test
{
    using System.Linq;
    using wave.stl;
    using wave.syntax;
    using Xunit;

    public class parse_test
    {
        public static WaveSyntax Wave => new();
        
        [Fact]
        public void CommentParseTest()
        {
            var a = new WaveSyntax();
            Assert.Equal(" bla ", a.CommentParser.AnyComment.ParseWave("/* bla */"));
            Assert.Equal(" bla", a.CommentParser.AnyComment.ParseWave("// bla"));
        }
        
        [Fact]
        public void IdentifierParseTest()
        {
            var a = new WaveSyntax();
            Assert.Equal("id", a.Identifier.ParseWave("id"));
            Assert.Throws<WaveParseException>(() => a.Identifier.ParseWave("4"));
            Assert.Throws<WaveParseException>(() => a.Identifier.ParseWave("public"));
        }
        
        [Theory]
        [InlineData(" a: int", "a", "int", 0, null)]
        [InlineData("  b : SomeClass", "b", "SomeClass", 0, null)]
        [InlineData("lst: List<T>", "lst", "List", 0, null)]
        [InlineData("readonly b: int", "b", "int", 1, "readonly")]
        [InlineData("static c: int", "c", "int", 1, "static")]
        public void ParameterDeclarationParseTest(string parseStr, string name, string type, int lenMod, string mod)
        {
            var result = new WaveSyntax().ParameterDeclaration.ParseWave(parseStr);
            
            Assert.Equal(name, result.Identifier);
            Assert.Equal(type, result.Type.Identifier);
            Assert.Equal(lenMod, result.Modifiers.Count);
            if(mod is not null)
                Assert.Equal(mod, result.Modifiers[0]);
        }
        
        [Theory]
        [InlineData(" a int")]
        [InlineData("bla!")]
        [InlineData("b@b")]
        [InlineData("b@b: int")]
        [InlineData("b@b: int : int")]
        [InlineData("43534")]
        [InlineData("):s")]
        public void ParameterDeclarationParseTestFail(string parseStr)
        {
            Assert.Throws<WaveParseException>(() => new WaveSyntax().ParameterDeclaration.ParseWave(parseStr));
        }
        [Fact]
        public void MethodParametersAndBodyTest()
        {
            var a = new WaveSyntax();
            var d = a.OperationDeclaration
                .ParseWave(@"operation test[x: int32] -> int32 
                    {
                        body 
                        { 
                            return 1;
                        }
                        gc auto;
                    }");
            Assert.Equal("test", d.Identifier);
            Assert.Equal("int32", d.ReturnType.Identifier);
            Assert.Equal(SyntaxType.ReturnStatement, d.Body.Statements.First().Kind);
        }
        
        [Theory]
        [InlineData("operation test[x: int32] -> int32", "test", false)]
        [InlineData("operation test[] -> foo22", "test", false)]
        [InlineData("operation asd_d2[] -> foo22", "asd_d2", false)]
        [InlineData("operation asd_d2[i: s, x: w] -> foo22", "asd_d2", false)]
        [InlineData("operation asd-d[i: s, x: w] -> foo22", "asd-d2", true)]
        [InlineData("operation 123[i: s, x: w] -> foo22", "123", true)]
        [InlineData("operation @[i: s, x: w] -> foo22", "@", true)]
        [InlineData("operation name[ s s s s] -> foo22", "name", true)]
        [InlineData("operation name[i: s, x: w] - foo22", "name", true)]
        [InlineData("operation name[i: s, x: w]", "name", true)]
        public void MethodParametersTest(string parseStr, string name, bool needFail)
        {
            var a = new WaveSyntax();
            var d = default(MethodDeclarationSyntax);
            
            if (needFail)
            {
                Assert.Throws<WaveParseException>(() =>
                {
                    d = a.OperationDeclaration
                        .ParseWave(parseStr + "{body{}}");
                });
            }
            else
            {
                d = a.OperationDeclaration
                    .ParseWave(parseStr + "{body{}}");
                Assert.Equal(name, d.Identifier);
            }
        }
        [Fact]
        public void UseDirectiveTest()
        {
            var a = new WaveSyntax();
            var d = a.UseSyntax
                .ParseWave("#use \"stl.lib\"");
            Assert.Equal("stl.lib", d.Value.UnwrapToken());
        }
        
        [Theory]
        [InlineData("class")]
        [InlineData("public")]
        [InlineData("private")]
        [InlineData("static")]
        [InlineData("auto")]
        public void KeywordIsNotIdentifier(string key) 
            => Assert.Throws<WaveParseException>(() => Wave.Identifier.ParseWave(key));
        
        
        [Theory]
        [InlineData("class", null, true)]
        [InlineData("true", "true", false)]
        [InlineData("1.23", "1.23", false)]
        [InlineData("FALSE", "false", false)]
        [InlineData("NULL", "null", false)]
        [InlineData("\"foo\\rbar\\n\"", "\"foo\\rbar\\n\"", false)]
        [InlineData("\"bla\"// the comment", "\"bla\"", false)]
        [InlineData("", null, true)]
        public void LiteralExpression(string parseStr, string result, bool needFail)
        {
            var expr = default(LiteralExpressionSyntax);
            if (needFail)
            {
                Assert.Throws<WaveParseException>(() =>
                {
                    expr = Wave.LiteralExpression.ParseWave(parseStr);
                });
            }
            else
            {
                expr = Wave.LiteralExpression.ParseWave(parseStr);
                Assert.NotNull(expr);
                Assert.Equal(result, expr.Token);
            }
        }
        
    }
}