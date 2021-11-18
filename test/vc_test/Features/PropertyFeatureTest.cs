namespace veinc_test.Features;

public class PropertyFeatureTest : TestContext
{
    [Test]
    public void PropertyTest()
    {
        var result = Syntax.PropertyDeclaration.ParseVein("public MaxValue: Int16 { get; set; }");

        Assert.AreEqual("MaxValue", result.Identifier.ToString());
        Assert.AreEqual("Int16", result.Type.Identifier.ToString());
        Assert.True(result.Modifiers.Any(x => x.ModificatorKind == ModificatorKind.Public));
        Assert.NotNull(result.Setter);
        Assert.NotNull(result.Getter);
        Assert.True(result.Getter.IsGetter);
        Assert.True(result.Setter.IsSetter);
        Assert.True(result.Getter.IsEmpty);
        Assert.True(result.Setter.IsEmpty);

        result = Syntax.PropertyDeclaration.ParseVein("public MaxValue: Int16 { get; }");

        Assert.True(result.Getter.IsEmpty);
        Assert.Null(result.Setter);

        result = Syntax.PropertyDeclaration.ParseVein("public MaxValue: Int16 { get; private set; }");
        Assert.True(result.Getter.IsEmpty);
        Assert.NotNull(result.Setter);
        Assert.True(result.Setter.Modifiers.Any(x => x.ModificatorKind == ModificatorKind.Private));
    }
}
