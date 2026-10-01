namespace veinc_test.Features;

using System.Collections.Generic;

/// <summary>
/// Metadata round-trip for generic classes: the declared type-parameter arity is serialized in the
/// class record and reconstructed by <see cref="ModuleReader.DecodeClass"/>.
/// </summary>
public class GenericMetadataTest : TestContext
{
    private static VeinModuleBuilder CreateModule(string name = "gen_meta_test")
        => new(new ModuleNameSymbol(name), Types.Storage);

    private static VeinClass RoundTrip(VeinModuleBuilder module, ClassBuilder @class)
    {
        // Give the class a method so BakeByteArray emits a non-empty body.
        var m = @class.DefineMethod("dummy", MethodFlags.Public | MethodFlags.Static,
            VeinTypeCode.TYPE_VOID.AsClass()(Types.Storage));
        m.GetGenerator().Emit(OpCodes.RET);

        var baked = ((IBaker)@class).BakeByteArray();
        Assert.IsNotEmpty(baked);

        var reader = new ModuleReader_TestHelper(Types.Storage, module);
        return ModuleReader.DecodeClass(baked, reader, new List<(VeinClass, byte[])>());
    }

    [Test]
    public void SingleParam_ArityRoundTrips()
    {
        var module = CreateModule();
        var list = module.DefineClass(new NameSymbol("List<T>"), NamespaceSymbol.Internal);
        list.Flags = ClassFlags.Public;
        list.TypeArgs.Add(new VeinTypeArg("T"));
        list.DefineField("count", FieldFlags.Public, VeinTypeCode.TYPE_I4.AsClass()(Types.Storage));

        var decoded = RoundTrip(module, list);

        Assert.AreEqual(1, decoded.TypeArgs.Count);
        Assert.IsTrue(decoded.IsGenericType);
        Assert.AreEqual(1, decoded.Fields.Count);
        Assert.AreEqual(VeinTypeCode.TYPE_I4.AsClass()(Types.Storage).FullName, decoded.Fields[0].FieldType.Class.FullName);
    }

    [Test]
    public void MultiParam_ArityRoundTrips()
    {
        var module = CreateModule();
        var map = module.DefineClass(new NameSymbol("Map<TKey,TValue>"), NamespaceSymbol.Internal);
        map.Flags = ClassFlags.Public;
        map.TypeArgs.Add(new VeinTypeArg("TKey"));
        map.TypeArgs.Add(new VeinTypeArg("TValue"));
        map.DefineField("count", FieldFlags.Public, VeinTypeCode.TYPE_I4.AsClass()(Types.Storage));

        var decoded = RoundTrip(module, map);

        Assert.AreEqual(2, decoded.TypeArgs.Count);
        Assert.IsTrue(decoded.IsGenericType);
    }

    [Test]
    public void Constraint_Bittable_RoundTrips()
    {
        var module = CreateModule();
        var box = module.DefineClass(new NameSymbol("Box<T>"), NamespaceSymbol.Internal);
        box.Flags = ClassFlags.Public;
        box.TypeArgs.Add(new VeinTypeArg("T", new List<VeinBaseConstraint> { new VeinBaseConstraintConstBittable() }));
        box.DefineField("count", FieldFlags.Public, VeinTypeCode.TYPE_I4.AsClass()(Types.Storage));

        var decoded = RoundTrip(module, box);

        Assert.AreEqual(1, decoded.TypeArgs.Count);
        Assert.AreEqual("T", decoded.TypeArgs[0].Name); // the real name survives, not a synthetic "T0"
        Assert.AreEqual(1, decoded.TypeArgs[0].Constraints.Count);
        Assert.IsInstanceOf<VeinBaseConstraintConstBittable>(decoded.TypeArgs[0].Constraints[0]);
    }

    [Test]
    public void Constraint_Type_RoundTrips()
    {
        var module = CreateModule();
        var i4 = VeinTypeCode.TYPE_I4.AsClass()(Types.Storage);
        var box = module.DefineClass(new NameSymbol("Box<T>"), NamespaceSymbol.Internal);
        box.Flags = ClassFlags.Public;
        box.TypeArgs.Add(new VeinTypeArg("T", new List<VeinBaseConstraint> { new VeinBaseConstraintConstType(i4) }));
        box.DefineField("count", FieldFlags.Public, i4);

        var decoded = RoundTrip(module, box);

        Assert.AreEqual(1, decoded.TypeArgs[0].Constraints.Count);
        var constraint = decoded.TypeArgs[0].Constraints[0] as VeinBaseConstraintConstType;
        Assert.IsNotNull(constraint);
        Assert.AreEqual(i4.FullName, constraint.classes.FullName);
    }

    [Test]
    public void Constraint_MultiParam_RoundTrips()
    {
        var module = CreateModule();
        var map = module.DefineClass(new NameSymbol("Map<TKey,TValue>"), NamespaceSymbol.Internal);
        map.Flags = ClassFlags.Public;
        map.TypeArgs.Add(new VeinTypeArg("TKey", new List<VeinBaseConstraint> { new VeinBaseConstraintConstBittable() }));
        map.TypeArgs.Add(new VeinTypeArg("TValue", new List<VeinBaseConstraint> { new VeinBaseConstraintConstClass() }));
        map.DefineField("count", FieldFlags.Public, VeinTypeCode.TYPE_I4.AsClass()(Types.Storage));

        var decoded = RoundTrip(module, map);

        Assert.AreEqual(2, decoded.TypeArgs.Count);
        Assert.AreEqual("TKey", decoded.TypeArgs[0].Name);
        Assert.IsInstanceOf<VeinBaseConstraintConstBittable>(decoded.TypeArgs[0].Constraints[0]);
        Assert.AreEqual("TValue", decoded.TypeArgs[1].Name);
        Assert.IsInstanceOf<VeinBaseConstraintConstClass>(decoded.TypeArgs[1].Constraints[0]);
    }

    [Test]
    public void Constraint_EnforcedOnLoadedClass()
    {
        // The cross-module path: bake a constrained generic class, decode it exactly as the dependency
        // loader does, then enforce the constraint against concrete arguments. Proves a constraint that
        // arrives via a loaded .wll (not same-module source) is honoured.
        var module = CreateModule();
        var box = module.DefineClass(new NameSymbol("Box<T>"), NamespaceSymbol.Internal);
        box.Flags = ClassFlags.Public;
        box.TypeArgs.Add(new VeinTypeArg("T", new List<VeinBaseConstraint> { new VeinBaseConstraintConstBittable() }));
        box.DefineField("count", FieldFlags.Public, VeinTypeCode.TYPE_I4.AsClass()(Types.Storage));

        var decoded = RoundTrip(module, box);

        VeinComplexType stringArg = VeinTypeCode.TYPE_STRING.AsClass()(Types.Storage);
        VeinComplexType i32Arg = VeinTypeCode.TYPE_I4.AsClass()(Types.Storage);

        Assert.AreEqual(1, GenericConstraintChecker.CheckViolations(decoded, new List<VeinComplexType> { stringArg }).Count,
            "string must violate the bittable constraint carried by the loaded class");
        Assert.IsEmpty(GenericConstraintChecker.CheckViolations(decoded, new List<VeinComplexType> { i32Arg }),
            "i32 satisfies the bittable constraint carried by the loaded class");
    }

    [Test]
    public void NonGenericClass_ZeroArity()
    {
        var module = CreateModule();
        var foo = module.DefineClass(new NameSymbol("Foo"), NamespaceSymbol.Internal);
        foo.Flags = ClassFlags.Public;
        foo.DefineField("x", FieldFlags.Public, VeinTypeCode.TYPE_I4.AsClass()(Types.Storage));

        var decoded = RoundTrip(module, foo);

        Assert.AreEqual(0, decoded.TypeArgs.Count);
        Assert.IsFalse(decoded.IsGenericType);
    }
}
