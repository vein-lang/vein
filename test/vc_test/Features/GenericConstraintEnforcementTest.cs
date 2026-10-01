namespace veinc_test.Features;

using System.Collections.Generic;


/// <summary>
/// Enforcement of generic type-parameter constraints at a construction site
/// (<see cref="GenericConstraintChecker.CheckViolations"/>): bittable / class / type constraints are
/// validated against concrete arguments, open (parameter) arguments are skipped, and a constraint-less
/// or loaded definition produces no violations.
/// </summary>
public class GenericConstraintEnforcementTest : TestContext
{
    private static VeinClass I4 => VeinTypeCode.TYPE_I4.AsClass()(Types.Storage);
    private static VeinClass Str => VeinTypeCode.TYPE_STRING.AsClass()(Types.Storage);

    private static VeinClass GenericDef(string name, params VeinBaseConstraint[] constraints)
    {
        var module = new VeinModuleBuilder(new ModuleNameSymbol("constraint_test"), Types.Storage);
        var cls = module.DefineClass(new NameSymbol(name), NamespaceSymbol.Internal);
        cls.Flags = ClassFlags.Public;
        cls.TypeArgs.Add(new VeinTypeArg("T", new List<VeinBaseConstraint>(constraints)));
        return cls;
    }

    private static List<VeinComplexType> Args(params VeinComplexType[] a) => new(a);

    [Test]
    public void Bittable_SatisfiedByPrimitive()
        => Assert.IsEmpty(GenericConstraintChecker.CheckViolations(
            GenericDef("Box<T>", new VeinBaseConstraintConstBittable()), Args(I4)));

    [Test]
    public void Bittable_ViolatedByString()
    {
        var v = GenericConstraintChecker.CheckViolations(
            GenericDef("Box<T>", new VeinBaseConstraintConstBittable()), Args(Str));
        Assert.AreEqual(1, v.Count);
        StringAssert.Contains("bittable", v[0]);
    }

    [Test]
    public void Class_SatisfiedByReferenceType()
        => Assert.IsEmpty(GenericConstraintChecker.CheckViolations(
            GenericDef("Box<T>", new VeinBaseConstraintConstClass()), Args(Str)));

    [Test]
    public void Class_ViolatedByValueType()
    {
        var v = GenericConstraintChecker.CheckViolations(
            GenericDef("Box<T>", new VeinBaseConstraintConstClass()), Args(I4));
        Assert.AreEqual(1, v.Count);
        StringAssert.Contains("reference type", v[0]);
    }

    [Test]
    public void NoConstraint_NeverViolates()
    {
        var def = GenericDef("Box<T>");
        Assert.IsEmpty(GenericConstraintChecker.CheckViolations(def, Args(I4)));
        Assert.IsEmpty(GenericConstraintChecker.CheckViolations(def, Args(Str)));
    }

    [Test]
    public void OpenParameterArgument_IsSkipped()
    {
        // Box<T> with a bittable constraint, instantiated with a bare parameter U (still open):
        // there is nothing concrete to validate at this site, so no violation is produced.
        VeinComplexType openArg = new VeinTypeArg("U");
        Assert.IsEmpty(GenericConstraintChecker.CheckViolations(
            GenericDef("Box<T>", new VeinBaseConstraintConstBittable()), Args(openArg)));
    }
}
