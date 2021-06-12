namespace wc_test
{
    using System;
    using Sprache;
    using System.Linq;
    using insomnia;
    using mana.stl;
    using mana.syntax;
    using Xunit;
    using Xunit.Abstractions;

    public class parse_test
    {
        private readonly ITestOutputHelper _logger;

        public parse_test(ITestOutputHelper logger)
        {
            _logger = logger;
            ManaParserExtensions._log = s => _logger.WriteLine(s);
        }

        public static ManaSyntax Mana => new();

        [Fact]
        public void CommentParseTest()
        {
            var a = new ManaSyntax();
            Assert.Equal(" bla ", a.CommentParser.AnyComment.ParseMana("/* bla */"));
            Assert.Equal(" bla", a.CommentParser.AnyComment.ParseMana("// bla"));
        }
        [Fact]
        public void VariableStatementTest()
        {
            var result = Mana.Block.End().ParseMana(@"{
    auto f = 12;

    return 1;
    return 2;
}");
            Assert.False(result.IsBrokenToken);
            foreach (var statement in result.Statements)
                Assert.False(statement.IsBrokenToken);
        }

        [Fact]
        public void SingleVariableTest()
        {
            var result = Mana.VariableDeclaration.End().ParseMana($"auto f: Int32 = 12;");
        }

        [Fact]
        public void VariableInFunctionTest()
        {
            var result = Mana.MethodDeclaration.End().ParseMana(@"public Foo(): Int32 {
    auto f = 12;

    return f;
}");
            Assert.False(result.IsBrokenToken);
            var statements = result.Body.Statements;

            Assert.IsType<LocalVariableDeclaration>(statements[0]);
            Assert.IsType<ReturnStatementSyntax>(statements[1]);
        }

        [Fact]
        public void IdentifierParseTest()
        {
            var a = new ManaSyntax();
            Assert.Equal("id", a.Identifier.ParseMana("id"));
            Assert.Throws<ManaParseException>(() => a.Identifier.ParseMana("4"));
            Assert.Throws<ManaParseException>(() => a.Identifier.ParseMana("public"));
        }

        [Theory]
        [InlineData(" a: int", "a", "int", 0, null)]
        [InlineData("  b : SomeClass", "b", "SomeClass", 0, null)]
        [InlineData("lst: List<T>", "lst", "List", 0, null)]
        [InlineData("override b: int", "b", "int", 1, "override")]
        [InlineData("static c: int", "c", "int", 1, "static")]
        public void ParameterDeclarationParseTest(string parseStr, string name, string type, int lenMod, string mod)
        {
            var result = new ManaSyntax().ParameterDeclaration.ParseMana(parseStr);

            Assert.Equal(name, result.Identifier.ToString());
            Assert.Equal(type, result.Type.Identifier.ToString());
            Assert.Equal(lenMod, result.Modifiers.Count);
            if (mod is not null)
                Assert.Equal(mod, result.Modifiers[0].ModificatorKind.ToString().ToLower());
        }

        [Theory]
        [InlineData(" a int")]
        [InlineData("bla!")]
        [InlineData("b@b")]
        [InlineData("b@b: int")]
        [InlineData("b@b: int : int")]
        [InlineData("43534")]
        [InlineData("):s")]
        public void ParameterDeclarationParseTestFail(string parseStr) =>
            Assert.Throws<ManaParseException>(() => new ManaSyntax().ParameterDeclaration.ParseMana(parseStr));

        [Fact]
        public void OperationParametersAndBodyTest()
        {
            var a = new ManaSyntax();
            var d = a.OperationDeclaration
                .ParseMana(@"operation test[x: int32] -> int32 
                    {
                        body 
                        { 
                            return 1;
                        }
                        gc auto;
                    }");
            Assert.Equal("test", d.Identifier.ToString());
            Assert.Equal("int32", d.ReturnType.Identifier.ToString().ToLower());
            Assert.Equal(SyntaxType.ReturnStatement, d.Body.Statements.First().Kind);
        }
        [Fact]
        public void QualifiedExpressionNewTest()
            => Mana.QualifiedExpression.ParseMana("new Foo()");

        [Theory]
        [InlineData("foo: Type;")]
        [InlineData("[special] foo: Type;")]
        [InlineData("[special] public foo: Type;")]
        public void FieldTest00(string str)
            => Mana.FieldDeclaration.ParseMana(str);

        [Fact]
        public void InvalidTokenFieldParse()
        {
            var result = Mana.CompilationUnit.ParseMana(
                @"#space ""wave/lang""
                [special]
                public class String : Object {
                [native]
                private _value: String;
                [native]
                public extern this*^*index: int32]: Char;
                [native]
                public extern Length: Int32; }
                ");

            var clazz = result.Members.First() as ClassDeclarationSyntax;
            Assert.NotNull(clazz);
            var errMember = clazz.Members.Skip(1).First();
            Assert.True(errMember.IsBrokenToken);
        }

        [Fact]
        public void MethodParametersAndBodyTest()
        {
            var a = new ManaSyntax();
            var d = a.MethodDeclaration
                .ParseMana("public test(x: int32): void { }");
            Assert.Equal("test", d.Identifier.ToString());
            Assert.Equal("void", d.ReturnType.Identifier.ToString().ToLower());
        }

        [Fact]
        public void FooAoo()
        {
            AppFlags.Set("exp_simplify_optimize", false);
            var result = Mana.QualifiedExpression.End().ParseMana("1 + 2 - 3 * 4 / 5 ^^ 2");
            _logger.WriteLine(result.ToString());
        }

        [Fact]
        public void FullsetMethodParametersAndBodyTest()
        {
            var a = new ManaSyntax();
            var d = a.ClassDeclaration
                .ParseMana("public class DDD { public test(x: int32): void { } }");

            Assert.False(d.IsStruct);
            Assert.False(d.IsInterface);
            Assert.Equal("DDD", d.Identifier.ToString());
            Assert.Contains(d.Modifiers, x => x.ModificatorKind == ModificatorKind.Public);
            var method = d.Methods.Single();
            Assert.Equal("test", method.Identifier.ToString());
            Assert.Contains(method.Modifiers, x => x.ModificatorKind == ModificatorKind.Public);
            Assert.Equal("void", method.ReturnType.Identifier.ToString().ToLower());

            var @params = method.Parameters.Single();
            Assert.Equal("x", @params.Identifier.ToString());
            Assert.Equal("int32", @params.Type.Identifier.ToString().ToLower());
        }


        [Theory]
        [InlineData("operation test[x: int32] -> int32", "test", false)]
        [InlineData("operation test[] -> foo22", "test", false)]
        [InlineData("operation asd_d2[] -> foo22", "asd_d2", false)]
        [InlineData("operation asd_d2[i: s, x: w] -> foo22", "asd_d2", false)]
        [InlineData("operation asd-d[i: s, x: w] -> foo22", "asd-d2", true)]
        [InlineData("operation 123[i: s, x: w] -> foo22", "123", true)]
        [InlineData("operation $[i: s, x: w] -> foo22", "$", true)]
        [InlineData("operation name[ s s s s] -> foo22", "name", true)]
        [InlineData("operation name[i: s, x: w] - foo22", "name", true)]
        [InlineData("operation name[i: s, x: w]", "name", true)]
        public void MethodParametersTest(string parseStr, string name, bool needFail)
        {
            var a = new ManaSyntax();
            var d = default(MethodDeclarationSyntax);

            if (needFail)
            {
                Assert.Throws<ManaParseException>(() =>
                {
                    d = a.OperationDeclaration
                        .ParseMana(parseStr + "{body{}}");
                });
            }
            else
            {
                d = a.OperationDeclaration
                    .ParseMana(parseStr + "{body{}}");
                Assert.Equal(name, d.Identifier.ToString());
            }
        }
        [Fact]
        public void UseDirectiveTest()
        {
            var a = new ManaSyntax();
            var d = a.UseSyntax
                .ParseMana("#use \"stl.lib\"") as UseSyntax;
            Assert.Equal("stl.lib", d.Value.Token);
        }

        [Fact]
        public void SpaceDirectiveTest()
        {
            var a = new ManaSyntax();
            var d = a.SpaceSyntax
                .ParseMana("#space \"foo\"");
            Assert.Equal("foo", d.Value.Token);
        }

        [Fact]
        public void AnnotationTest()
        {
            var a = new ManaSyntax();
            var d = a.AnnotationExpression.End().ParseMana("[special, native]");
            Assert.Equal(2, d.Length);
        }
        [Fact]
        public void MemberFailTest()
        {
            var result =
                Mana.ClassMemberDeclaration.End().ParseMana("public const MaxValue Int16 = 32767;");
            _logger.WriteLine($"type: {result.GetType()}");
        }
        [Fact]
        public void FieldTest()
            => Mana.FieldDeclaration.ParseMana("public const MaxValue: Int16 = 32767;");

        [Fact]
        public void FieldWithAnnotationTest()
            => Mana.FieldDeclaration.ParseMana("[native] private _value: Int16;");

        [Fact]
        public void ExpressionTest()
        {
            var result = Mana.Statement.ParseMana(@"return (this.$indexer.at(Length - 1) == value);");
            Assert.True(result.IsBrokenToken);
            result = Mana.Statement.ParseMana(@"return (this.indexer.at(Length - 1) == value);");
            Assert.False(result.IsBrokenToken);
        }
        [Fact]
        public void MethodTest00()
        {
            Mana.MethodDeclaration.ParseMana(@"public EndsWith(value: Char): Boolean
            {
                if (Length - 1 < Length)
                    return false;
                return this == value;
            }");
        }
        [Fact]
        public void ReturnParseTest00()
        {
            Assert.False(Mana.Statement.ParseMana(@"return this == value;").IsBrokenToken);
        }

        [Theory]
        [InlineData("class")]
        [InlineData("public")]
        [InlineData("private")]
        [InlineData("static")]
        [InlineData("auto")]
        public void KeywordIsNotIdentifier(string key)
            => Assert.Throws<ManaParseException>(() => Mana.Identifier.ParseMana(key));

        [Theory]
        [InlineData("foo.bar.zet", 3)]
        [InlineData("foo.bar.zet()", 4)]
        [InlineData("zet()", 2)]
        [InlineData("zet++", 2)]
        [InlineData("zet->foo()", 3)]
        [InlineData("zet.foo()[2]", 4)]
        public void MemberOrMethodTest(string key, int chainLen)
        {
            var result = Mana.QualifiedExpression.End().ParseMana(key) as MemberAccessExpression;

            Assert.NotNull(result);

            var chain = result.GetChain().ToArray();


            Assert.Equal(chainLen, chain.Length);
        }
        [Fact]
        public void ValidateChainMember()
        {
            var key = $"zet.foo()[2]";
            var result = Mana.QualifiedExpression.End().ParseMana(key) as MemberAccessExpression;

            Assert.NotNull(result);

            var chain = result.GetChain().ToArray();


            Assert.IsType<IdentifierExpression>(chain[0]); // zet
            Assert.IsType<IdentifierExpression>(chain[1]); // foo
            Assert.IsType<MethodInvocationExpression>(chain[2]); // ()
            Assert.IsType<BracketExpression>(chain[3]); // []
        }
        [Theory]
        [InlineData("class", null, true)]
        [InlineData("true", "true", false)]
        [InlineData("1.23f", "1.23", false)]
        [InlineData("1.23m", "1.23", false)]
        [InlineData("1.23d", "1.23", false)]
        [InlineData("1.23w", null, true)]
        [InlineData("144", "144", false)]
        [InlineData("2147483647", "2147483647", false)]
        [InlineData("FALSE", "false", false)]
        [InlineData("NULL", "null", false)]
        [InlineData("\"foo\\rbar\\n\"", "foo\\rbar\\n", false)]
        [InlineData("\"bla\"// the comment", "bla", false)]
        [InlineData("", null, true)]
        public void LiteralExpression(string parseStr, string result, bool needFail)
        {
            var expr = default(LiteralExpressionSyntax);
            if (needFail)
            {
                Assert.Throws<ManaParseException>(() =>
                {
                    expr = Mana.LiteralExpression.End().ParseMana(parseStr);
                });
            }
            else
            {
                expr = Mana.LiteralExpression.End().ParseMana(parseStr);
                Assert.NotNull(expr);
                Assert.Equal(result, expr.Token);
            }
        }
        [Fact]
        public void AccessMemberTest() => Mana.QualifiedExpression.End().ParseMana(@"44.govno");

        [Theory]
        [InlineData("new s()")] // new exp
        [InlineData("new c(1)")] // new exp
        [InlineData("new x(x)")] // new exp
        [InlineData("55.access")] // lit access member
        [InlineData("55 != 55")] // logic exp
        [InlineData("55 << 55")] // math exp
        [InlineData("55 * 55 / 2")] // math exp
        [InlineData("(x: Int32) |> 22")] // function lambda
        [InlineData("(x: Int32) |> {}")] // function lambda
        [InlineData("() |> null")] // function lambda
        [InlineData("22..22")] // range
        [InlineData("x..22")] // range
        [InlineData("22..x")] // range
        [InlineData("true ? 22 : 32")] // conditional exp
        [InlineData("a ? fail new foo() : 2")] // fail exp
        [InlineData("foo ?? 22")] // coalescing exp
        [InlineData("as Type")] // as exp
        [InlineData("is Type")] // is exp
        [InlineData("this.call()")] // this based call
        [InlineData("(1..i < 1 ? 1 : i)")]
        [InlineData("v1.x * v2.x + v1.y * v2.y + v1.z * v2.z")]
        [InlineData("Pf.x - 0.5f + jitter * ox")]
        [InlineData("mod7(floor((p * K))) + K - Ko")]
        [InlineData("(d1.x < d1.y) ? d1.xy : d1.yx")]
        [InlineData("permute(Pi.x + float3(-1.0f, 0.0f, 1.0f))")]
        [InlineData("int4(asint(x.x), asint(x.y), asint(x.z), asint(x.w))")]
        [InlineData("abs(x) < double.PositiveInfinity")]
        [InlineData("(asulong(x) & 07) > 07")]
        [InlineData("double.IsNaN(y) || x < y ? x : y")]
        [InlineData("x = ((x >> 1) & 055555555) | ((x & 055555555) << 1)")]
        [InlineData("uf = select(uf, asuint(asfloat(uf + (1 << 23)) - 6.10351563e-05f), e == 0)")]
        [InlineData("uf += select(0, (128u - 16u) << 23, e == shifted_exp)")]
        [InlineData("hx = (asuint(min(asfloat(uux) * 1.92592994e-34f, 260042752.0f)) + 0x1000) >> 13", Skip = "todo")]
        [InlineData("basis1.x = 1.0f + sign * normal.x * normal.x * a")]
        [InlineData("hash = rol(state.x, 1) + rol(state.y, 7) + rol(state.z, 12) + rol(state.w, 18)")]
        public void AllExpressionTest(string exp) => Mana.QualifiedExpression.End().ParseMana(exp);

        [Fact]
        public void NewExpTest()
        {
            var result = Mana.QualifiedExpression.End().ParseMana(@"new Foo()");
            Assert.IsType<NewExpressionSyntax>(result);

            var exp = result as NewExpressionSyntax;
            Assert.NotNull(exp);
            Assert.Equal(SyntaxType.NewExpression, exp.Kind);
            Assert.IsType<ObjectCreationExpression>(exp.CtorArgs);
            Assert.Equal("global::Foo", exp.TargetType.Typeword.GetFullName());
        }
        [Fact]
        public void ExponentPartTest()
        {
            Assert.Equal("e+23", Mana.ExponentPart.End().ParseMana("e+23"));
            Assert.Equal("e-23", Mana.ExponentPart.End().ParseMana("e-23"));
        }

        [Fact(Skip = "TODO")]
        public void LiteralAssignedExpressionTest()
        {
            var result = Mana.FieldDeclaration.End().ParseMana("foo: Int32 = -22;");
            Assert.NotNull(result);
            Assert.Equal("int32", result.Type.Identifier.ToString());
            Assert.Equal("foo", result.Field.Identifier.ToString());
            Assert.Equal("(-22)", result.Field.Expression.ExpressionString);
            Assert.IsType<UnaryExpressionSyntax>(result.Field.Expression);
        }
        [Theory]
        [InlineData("class")]
        [InlineData("struct")]
        [InlineData("interface")]
        public void DeclarationCanDeclareMethods(string keyword)
        {
            var cd = Mana.ClassDeclaration.Parse($"[special] {keyword} Program {{ [special] main(): void {{}} }}");
            Assert.True(cd.Methods.Any());
            Assert.Equal("Program", cd.Identifier.ToString());
            Assert.Equal(ManaAnnotationKind.Special, cd.Annotations.Single().AnnotationKind);

            var md = cd.Methods.Single();
            Assert.Equal("void", md.ReturnType.Identifier.ToString());
            Assert.Equal("main", md.Identifier.ToString());
            Assert.Equal(ManaAnnotationKind.Special, md.Annotations.Single().AnnotationKind);
            Assert.False(md.Parameters.Any());

            Assert.Throws<ManaParseException>(() => Mana.ClassDeclaration.ParseMana(" class Test { void Main }"));
            Assert.Throws<ManaParseException>(() => Mana.ClassDeclaration.ParseMana("class Foo { int main() }"));
        }
        [Fact]
        public void InheritanceTest()
        {
            var cd = Mana.ClassDeclaration.Parse("class Program : Object {}");

            Assert.Equal("Object", cd.Inheritances.Single().Identifier.ToString());
        }

        [Fact]
        public void FieldsTest()
        {
            var cd = Mana.ClassDeclaration.Parse("class Program : Object { foo: foo; }");

            Assert.Equal("Object", cd.Inheritances.Single().Identifier.ToString());
        }

        [Theory]
        [InlineData("foo()")]
        [InlineData("foo.bar()")]
        [InlineData("foo.bar.zoo()")]
        [InlineData("foo.bar.zoo(a, b)")]
        [InlineData("foo.bar.zoo(a, b, 4)")]
        [InlineData("foo.bar.zoo(a, b, 4, 4 + 4)")]
        [InlineData("foo.bar.zoo(a, b, 4, woo())")]
        [InlineData("foo.bar.zoo(a, b, 4, zak.woo())")]
        [InlineData("foo.bar.zoo(a, b, 4, zak.woo(a, b, 4))")]
        //[InlineData("global::foo.bar.zoo(a, b, 4, 4 + 4);")]
        public void InvocationTest(string parseStr)
            => Mana.QualifiedExpression.End().ParseMana(parseStr);
        [Theory]
        [InlineData("a")]
        [InlineData("b")]
        [InlineData("4")]
        [InlineData("woo()")]
        [InlineData("zak.woo()")]
        [InlineData("zak.woo(a, b, 4)")]
        public void ArgTest(string args)
            => Mana.argument.End().ParseMana(args);
        [Theory]
        [InlineData("a, b")]
        [InlineData("a, b, 4")]
        [InlineData("a, b, 4, 4 + 4")]
        [InlineData("a, b, 4, woo()")]
        [InlineData("a, b, 4, zak.woo()")]
        [InlineData("a, b, 4, zak.woo(a, b, 4)")]
        public void ArgListTest(string args)
            => Mana.argument_list.End().ParseMana(args);
        [Theory]
        [InlineData("foo);")]
        [InlineData("foo.bar(")]
        [InlineData("foo@bar.zoo(a, b)")]
        [InlineData("foo.bar.zoo(a b, 4)")]
        [InlineData("foo.bar.zoo(a, b, 4, 4 $ 4)")]
        public void InvocationTestFail(string parseStr)
            => Assert.Throws<ManaParseException>(() => Mana.QualifiedExpression.End().ParseMana(parseStr));

        [Fact]
        public void GenericExpressionTest()
        {
            var result = Mana.QualifiedExpression.ParseMana("foo.bar.zoo(zak.woo(a, b, c), a, b, c)");
        }

        [Theory]
        [InlineData("2 ^ 4 + 2 - 2 * 2 % 2 ^^ 2 == foo()")]
        [InlineData("!106 / ~27 ^ !81 / ~38 ^ ~65 - 202 ^ ~214 & ~143")]
        [InlineData("~16 ^ !243 && ~131 ^ 171 && 224 >> !67 && ~24 * ~235")]
        [InlineData("~107 & 178 ^^ ~111 || ~113 ^^ ~222 >> !100 ^^ 65 || ~94")]
        [InlineData("~119 ^^ ~161 ^^ ~89 * !241 ^^ ~131 && ~129 ^^ 83 ^^ 44")]
        [InlineData("!210 && !96 + ~71 - 34 + ~14 << ~164 + !217 * ~147")]
        [InlineData("222 ^ 78 + !138 & !192 + 211 / ~63 + ~168 ^^ !55")]
        [InlineData("!160 & ~138 ^^ !248 % ~240 ^^ 68 + !206 ^^ !31 || 81")]
        [InlineData("!173 / !120 ^^ 225 && 7 ^^ ~21 >> !238 ^^ ~197 ^^ !98")]
        [InlineData("185 / ~102 / !152 ^ !202 / ~32 ^^ !130 / ~164 & !58")]
        [InlineData("123 + 161 && ~96 * !138 && ~86 & ~158 && !83 ^^ 167")]
        [InlineData("!136 && 144 || 252 * ~123 || ~185 && ~207 || !171 ^ 234")]
        [InlineData("!226 / !79 / 105 * 28 / ~201 && ~205 / ~226 + 64")]
        [InlineData("~164 & !138 && !50 & !212 && 194 >> 34 && 25 * 67")]
        [InlineData("~176 / ~121 || 27 && 234 || 11 & 125 || 238 ^ 70")]
        [InlineData("~189 && !103 || ~4 >> ~2 || 1 - ~195 || !200 && !234")]
        [InlineData("~24 / ~39 / 241 >> 49 / ~127 + !172 / !1 >> ~30")]
        [InlineData("!217 & ~98 && ~186 >> 233 && !42 + 161 && !54 << ~34")]
        [InlineData("!230 / 80 || 18 + 163 || 154 || !192 || !70 && ~173")]
        [InlineData("~242 && ~62 || ~139 ^^ ~23 || !224 >> ~224 || !221 + !228")]
        [InlineData("!180 ^^ 121 % 118 % 85 % 163 / !2 % ~165 << ~73")]
        [InlineData("!193 && ~104 % ~61 ^ !228 % 127 && 23 % 116 / ~236")]
        [InlineData("~139 % !45 & ~130 ^^ 97 & 43 + !47 & 168 / ~84")]
        [InlineData("~230 >> ~236 ^ ~112 * !145 ^ !80 & ~105 ^ 25 << ~173")]
        [InlineData("~242 - !218 ^ ~89 % !168 ^ !112 & ~57 ^ ~14 & !154")]
        [InlineData("~0 << !200 ^ !65 + 190 ^ 140 && ~143 ^ ~143 ^^ ~47")]
        [InlineData("!193 % !5 & !10 % !118 & ~15 ^ !111 & !203 ^ ~28")]
        [InlineData("!205 << ~242 && ~242 + !141 && ~46 ^ !64 && !192 % 9")]
        [InlineData("218 ^ 224 && ~35 - 75 && !238 + !185 && ~137 & !117")]
        [InlineData("!156 >> !28 * 164 / ~92 * !234 % ~204 * ~186 && ~126")]
        [InlineData("!246 - 219 & ~169 << ~146 & !232 & ~238 & !17 << ~190")]
        [InlineData("!4 << !201 && !123 && 162 && 26 % ~14 && ~59 + !83")]
        [InlineData("~196 % 5 / ~15 - 68 / ~232 ^ 152 / !142 / !113")]
        [InlineData("209 & 243 * !251 >> ~129 * ~76 ^^ ~201 * 200 * !153")]
        [InlineData("!222 - ~225 * 21 ^^ ~135 * !250 || !203 * !219 / 101")]
        [InlineData("~57 ^ !161 && ~3 << ~183 && ~236 / !123 && !165 ^ ~144")]
        [InlineData("~250 - 220 * !82 ^ 203 * ~158 << ~119 * !142 << !53")]
        [InlineData("7 << !202 * 196 + ~180 * ~2 ^ ~168 * !230 >> 119")]
        [InlineData("~20 + 184 % !55 && !157 % !217 - !171 % ~185 || ~88")]
        [InlineData("!213 << !243 >> ~102 >> !84 >> !43 ^ ~231 >> !34 << 145")]
        [InlineData("!225 - 226 << !156 ^^ ~79 << 137 >> 74 << 142 - ~204")]
        [InlineData("!163 >> ~30 - 24 / ~35 - !7 << 201 - !2 || !4")]
        [InlineData("~1465335714 + 802595077 - ~1557282100 % ~1701421680")]
        [InlineData("~1571318926 ^^ !654000949 - ~1505232535 * ~1937377465")]
        [InlineData("~185449798 & 110682480 >> 1276287843 || 1353137228")]
        [InlineData("1808804974 % !608506720 % 1161111307 & !891268492")]
        [InlineData("!1914788186 + 459912586 - ~2123293822 << ~695079347")]
        [InlineData("~1390659715 << ~957736826 << ~233210611 / !1322441515")]
        [InlineData("!1496642927 + 809142698 ^ !822816154 % ~37021466")]
        [InlineData("!1602626139 ^ ~660548570 ^ ~1988315968 + !1699505167")]
        [InlineData("~1078497668 - ~1158372810 & 1526447232 || !1096150041")]
        [InlineData("~1184480880 ^ !1009778682 & !1330258087 - !1284681867")]
        [InlineData("~1946095399 + ~466460207 ^ 1178162780 << ~1687924343")]
        [InlineData("2052078611 && !317866079 ^^ !203526744 * 981973635")]
        [InlineData("666209483 >> !1922031251 - ~1107833619 ^^ !829878328")]
        [InlineData("142081012 / !272371844 ^ ~992657083 / ~368009592")]
        [InlineData("!248064224 & ~123777716 ^ !171820447 >> 1864875345")]
        [InlineData("!1871419400 ^ 621601962 & !1839663062 && !1857435358")]
        [InlineData("1977402612 % !473007828 & !654361930 ^ ~1661246213")]
        [InlineData("!2083385824 ^^ ~324413706 && !1465057068 << 1638583869")]
        [InlineData("~1559257353 + ~822237940 * 1003188332 - ~1035228745")]
        [InlineData("!1665240565 ^ !673643812 * ~806999187 >> ~1223760571")]
        [InlineData("-599501569 ^^ -1537610347 ^^ -1801974617 && !-943604673")]
        [InlineData("~-728565646 & ~-1896339527 && !-651565412 && ~-2116790075")]
        public void OperatorTest(string parseKey)
        {
            var result = Mana.QualifiedExpression.End().ParseMana($"({parseKey})");
            _logger.WriteLine(result?.ExpressionString);
        }

        [Theory]
        [InlineData("native(\"foo\")")]
        public void AnnotationWithArgsTest(string str)
        {
            var result = Mana.AnnotationSyntax.End().ParseMana(str);
            Assert.NotEmpty(result.Args);
        }

        [Fact]
        public void FooProgramTest()
        {
            Mana.CompilationUnit.End().ParseMana(
            "#use \"stl.lib\"\n" +
               "public class Foo {" +
               "public master(): void {}" +
               "}");
        }

        [Fact]
        public void CtorAndDtorTest()
        {
            Mana.CompilationUnit.End().ParseMana(
                "#use \"stl.lib\"\n" +
                "public class Foo {" +
                "public new() {}" +
                "}");
            Mana.CompilationUnit.End().ParseMana(
                "#use \"stl.lib\"\n" +
                "public class Foo {" +
                "public delete() {}" +
                "}");
            Mana.CompilationUnit.End().ParseMana(
                "#use \"stl.lib\"\n" +
                "public class Foo {" +
                "public new(s:S) {}" +
                "}");
        }
        [Fact]
        public void AssignVariableTest()
        {
            Mana.QualifiedExpression.End().ParseMana("x = x");
        }

        [Fact]
        public void MethodWithAssignVariableTest()
        {
            Mana.Statement.End().ParseMana("x = x;");
        }

        [Fact]
        public void FooUseTest()
        {
            Mana.UseSyntax.End().ParseMana("#use \"boo\"");
        }

        [Theory]
        [InlineData("this.x = 22")]
        [InlineData("this.x.w.d = 22")]
        [InlineData("this.x.w.d = zo.fo()")]
        [InlineData("this.x.w.d = zo.fo(1,2,3)")]
        [InlineData("this.x.w.d = zo.fo(1,2,3, this.x.w.d)")]
        public void ThisAccessTest(string str)
            => Mana.QualifiedExpression.End().ParseMana(str);
    }
}
