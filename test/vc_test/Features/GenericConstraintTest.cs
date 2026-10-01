namespace veinc_test.Features;

using System.Linq;

/// <summary>
/// Parsing of generic type-parameter constraints (<c>when T is ...</c>) and their conversion to the
/// runtime <see cref="VeinBaseConstraint"/> model. (Enforcement is a separate, later phase.)
/// </summary>
public class GenericConstraintTest : TestContext
{
    [Test]
    public void Parse_BittableConstraint()
    {
        var c = Syntax.GenericConstraintParser.ParseVein("when T is bittable").First();
        Assert.IsTrue(c.IsBittable);
        Assert.IsFalse(c.IsClass);

        var vc = c.ToConstraint(_ => null);
        Assert.IsInstanceOf<VeinBaseConstraintConstBittable>(vc);
        Assert.AreEqual(VeinTypeParameterConstraint.BITTABLE, vc.Constraint);
    }

    [Test]
    public void Parse_ClassConstraint()
    {
        var c = Syntax.GenericConstraintParser.ParseVein("when T is class").First();
        Assert.IsTrue(c.IsClass);
        Assert.IsFalse(c.IsBittable);

        var vc = c.ToConstraint(_ => null);
        Assert.IsInstanceOf<VeinBaseConstraintConstClass>(vc);
        Assert.AreEqual(VeinTypeParameterConstraint.CLASS, vc.Constraint);
    }

    [Test]
    public void Parse_TypeConstraint()
    {
        var c = Syntax.GenericConstraintParser.ParseVein("when T is i32").First();
        Assert.IsFalse(c.IsBittable);
        Assert.IsFalse(c.IsClass);
        Assert.AreEqual("i32", c.Constraint.Typeword.Identifier.ExpressionString);

        var i4 = VeinTypeCode.TYPE_I4.AsClass()(Types.Storage);
        var vc = c.ToConstraint(_ => i4);
        Assert.IsInstanceOf<VeinBaseConstraintConstType>(vc);
        Assert.AreEqual(VeinTypeParameterConstraint.TYPE, vc.Constraint);
        Assert.AreEqual(i4.FullName, ((VeinBaseConstraintConstType)vc).classes.FullName);
    }

    [Test]
    public void Parse_MultipleMixedConstraints()
    {
        var cs = Syntax.GenericConstraintParser.ParseVein("when T is bittable, T1 is class, T2 is i32").ToList();
        Assert.AreEqual(3, cs.Count);
        Assert.IsTrue(cs[0].IsBittable);
        Assert.IsTrue(cs[1].IsClass);
        Assert.AreEqual("i32", cs[2].Constraint.Typeword.Identifier.ExpressionString);
    }

    [Test]
    public void ConstraintEnum_FlagValues()
    {
        Assert.AreEqual(1 << 1, (int)VeinTypeParameterConstraint.TYPE);
        Assert.AreEqual(1 << 2, (int)VeinTypeParameterConstraint.BITTABLE);
        Assert.AreEqual(1 << 3, (int)VeinTypeParameterConstraint.SIGNATURE);
        Assert.AreEqual(1 << 4, (int)VeinTypeParameterConstraint.CLASS);
    }

    [Test]
    public void ClassDeclaration_CarriesConstraint()
    {
        var cd = Syntax.ClassDeclaration.ParseVein("public class Box<T> when T is bittable { value: T; }");
        Assert.IsTrue(cd.IsGeneric);
        Assert.NotZero(cd.TypeParameterConstraints.Count);
        Assert.IsTrue(cd.TypeParameterConstraints.First().IsBittable);
    }
}
