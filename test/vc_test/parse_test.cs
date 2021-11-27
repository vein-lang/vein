namespace veinc_test
{
    using System;
    using Sprache;
    using System.Linq;
    using vein.stl;
    using vein.syntax;
    using NUnit.Framework;
    using vein;

    public class parse_test
    {
        public static VeinSyntax VeinAst => new();

        [Test]
        public void CommentParseTest()
        {
            Assert.AreEqual(" bla ", VeinAst.CommentParser.AnyComment.ParseVein("/* bla */"));
            Assert.AreEqual(" bla", VeinAst.CommentParser.AnyComment.ParseVein("// bla"));
        }


        [Test]
        public void IdentifierParseTest()
        {
            var a = new VeinSyntax();
            Assert.AreEqual("id", $"{a.IdentifierExpression.ParseVein("id")}");
            Assert.Throws<VeinParseException>(() => a.IdentifierExpression.ParseVein("4"));
            Assert.IsTrue(a.IdentifierExpression.ParseVein("public").IsBrokenToken);
        }



        [Test]
        public void OperationParametersAndBodyTest()
        {
            var a = new VeinSyntax();
            var d = a.OperationDeclaration
                .ParseVein(@"operation test[x: int32] -> int32 
                    {
                        body 
                        { 
                            return 1;
                        }
                        gc auto;
                    }");
            Assert.AreEqual("test", d.Identifier.ToString());
            Assert.AreEqual("int32", d.ReturnType.Identifier.ToString().ToLower());
            Assert.AreEqual(SyntaxType.ReturnStatement, d.Body.Statements.First().Kind);
        }




        [Test, Ignore("broken OnPreview parser operator")]
        public void InvalidTokenFieldParse()
        {
            var result = VeinAst.CompilationUnit.ParseVein(
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

        //[Test]
        //public void MethodParametersAndBodyTest()
        //{
        //    var a = new VeinSyntax();
        //    var d = a.MethodDeclaration
        //        .ParseVein("public test(x: int32): void { }");
        //    Assert.AreEqual("test", d.Identifier.ToString());
        //    Assert.AreEqual("void", d.ReturnType.Identifier.ToString().ToLower());
        //}












        [Test]
        public void MemberFailTest() => Assert.Catch<VeinParseException>(() =>
            VeinAst.ClassMemberDeclaration.End().ParseVein("public const MaxValue Int16 = 32767;"));







        [Test]
        public void ExpressionTest()
        {
            var result = VeinAst.Statement.ParseVein(@"return (this.$indexer.at(Length - 1) == value);");
            Assert.True(result.IsBrokenToken);
            result = VeinAst.Statement.ParseVein(@"return (this.indexer.at(Length - 1) == value);");
            Assert.False(result.IsBrokenToken);
        }
        //[Test]
        //public void MethodTest00() =>
        //    VeinAst.MethodDeclaration.ParseVein(@"public EndsWith(value: Char): Boolean
        //    {
        //        if (Length - 1 < Length)
        //            return false;
        //        return this == value;
        //    }");

        [Test]
        public void ReturnParseTest00() => Assert.False(VeinAst.Statement.ParseVein(@"return this == value;").IsBrokenToken);

        [Theory]
        [TestCase("class")]
        [TestCase("public")]
        [TestCase("private")]
        [TestCase("static")]
        [TestCase("auto")]
        public void KeywordIsNotIdentifier(string key)
            => Assert.IsTrue(VeinAst.IdentifierExpression.ParseVein(key).IsBrokenToken);

        [Test]
        public void MemberNormalizedChainTest()
        {

            var r1 = VeinAst.primary_expression.End().ParseVein("foo");
            var r2 = VeinAst.primary_expression.End().ParseVein("foo.bar");
            var r3 = VeinAst.primary_expression.End().ParseVein("foo.bar.zet");
            var r4 = VeinAst.primary_expression.End().ParseVein("foo.bar.zet.gota");
            var r5 = VeinAst.primary_expression.End().ParseVein("foo.bar.zet.gota()");
            var r6 = VeinAst.primary_expression.End().ParseVein("gota().asd");
            // var chain = (result.Expression as MemberAccessExpression).GetNormalizedChain().ToArray();
        }

        [Theory]
        [TestCase("class", null, true)]
        [TestCase("true", "true", false)]
        [TestCase("1.23f", "1.23", false)]
        [TestCase("1.23m", "1.23", false)]
        [TestCase("1.23d", "1.23", false)]
        [TestCase("1.23w", null, true)]
        [TestCase("144", "144", false)]
        [TestCase("2147483647", "2147483647", false)]
        [TestCase("FALSE", "false", false)]
        [TestCase("NULL", "null", false)]
        [TestCase("\"foo\\rbar\\n\"", "foo\\rbar\\n", false)]
        [TestCase("\"bla\"// the comment", "bla", false)]
        [TestCase("", null, true)]
        public void LiteralExpression(string parseStr, string result, bool needFail)
        {
            var expr = default(LiteralExpressionSyntax);
            if (needFail)
            {
                Assert.Throws<VeinParseException>(() => expr = VeinAst.LiteralExpression.End().ParseVein(parseStr));
            }
            else
            {
                expr = VeinAst.LiteralExpression.End().ParseVein(parseStr);
                Assert.NotNull(expr);
                Assert.AreEqual(result, expr.Token);
            }
        }





        [Test]
        public void ExponentPartTest()
        {
            Assert.AreEqual("e+23", VeinAst.ExponentPart.End().ParseVein("e+23"));
            Assert.AreEqual("e-23", VeinAst.ExponentPart.End().ParseVein("e-23"));
        }



        [Test]
        public void InheritanceTest()
        {
            var cd = VeinAst.ClassDeclaration.Parse("class Program : Object {}");

            Assert.AreEqual("Object", cd.Inheritances.Single().Identifier.ToString());
        }

        [Test]
        public void FieldsTest()
        {
            var cd = VeinAst.ClassDeclaration.Parse("class Program : Object { foo: foo; }");

            Assert.AreEqual("Object", cd.Inheritances.Single().Identifier.ToString());
        }


        [Theory]
        [TestCase("a")]
        [TestCase("b")]
        [TestCase("4")]
        [TestCase("woo()")]
        [TestCase("zak.woo()")]
        [TestCase("zak.woo(a, b, 4)")]
        public void ArgTest(string args)
            => VeinAst.argument.End().ParseVein(args);
        [Theory]
        [TestCase("a, b")]
        [TestCase("a, b, 4")]
        [TestCase("a, b, 4, 4 + 4")]
        [TestCase("a, b, 4, woo()")]
        [TestCase("a, b, 4, zak.woo()")]
        [TestCase("a, b, 4, zak.woo(a, b, 4)")]
        public void ArgListTest(string args)
            => VeinAst.argument_list.End().ParseVein(args);








        [Test]
        public void MethodWithAssignVariableTest() => VeinAst.Statement.End().ParseVein("x = x;");


    }
}
