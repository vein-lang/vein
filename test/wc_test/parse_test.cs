namespace wc_test
{
    using NUnit.Framework;
    using wave.stl;

    public class parse_test
    {
        [Test]
        public void CommentParseTest()
        {
            var a = new WaveSyntax();
            Assert.AreEqual(" bla ", a.CommentParser.AnyComment.ParseEx("/* bla */"));
            Assert.AreEqual(" bla", a.CommentParser.AnyComment.ParseEx("// bla"));
        }
        
        [Test]
        public void IdentifierParseTest()
        {
            var a = new WaveSyntax();
            Assert.AreEqual("id", a.Identifier.ParseEx("id"));
            Assert.Throws<WaveParseException>(() => a.Identifier.ParseEx("4"));
            Assert.Throws<WaveParseException>(() => a.Identifier.ParseEx("public"));
        }
        
        [Test]
        public void ParameterDeclarationParseTest()
        {
            var a = new WaveSyntax();
            var pd = a.ParameterDeclaration.ParseEx(" int a");
            Assert.AreEqual("int", pd.Type.Identifier);
            Assert.AreEqual("a", pd.Identifier);
            Assert.AreEqual(0, pd.Modifiers.Count);

            pd = a.ParameterDeclaration.ParseEx(" SomeClass b");
            Assert.AreEqual("SomeClass", pd.Type.Identifier);
            Assert.AreEqual("b", pd.Identifier);
            Assert.AreEqual(0, pd.Modifiers.Count);

            pd = a.ParameterDeclaration.ParseEx(" List<string> stringList");
            Assert.AreEqual("List", pd.Type.Identifier);
            Assert.AreEqual(1, pd.Type.TypeParameters.Count);
            Assert.AreEqual("string", pd.Type.TypeParameters[0].Identifier);
            Assert.AreEqual("stringList", pd.Identifier);
            Assert.AreEqual(0, pd.Modifiers.Count);

            pd = a.ParameterDeclaration.ParseEx(" readonly int a");
            Assert.AreEqual("int", pd.Type.Identifier);
            Assert.AreEqual("a", pd.Identifier);
            Assert.AreEqual(1, pd.Modifiers.Count);
            Assert.AreEqual("readonly", pd.Modifiers[0]);

            Assert.Throws<WaveParseException>(() => a.ParameterDeclaration.ParseEx("Hello!"));
        }
        [Test]
        public void MethodParametersAndBodyTest()
        {
            var a = new WaveSyntax();
            var d = a.OperationDeclaration
                .ParseEx(@"operation test[] -> int32 
                    {
                        body 
                        { 
                            return 1;
                        }
                        gc auto;
                    }");
            Assert.AreEqual("", d.Identifier);
        }
    }
}