namespace veinc_test.Features;

using System.Collections.Generic;

/// <summary>
/// Unit tests for the generic type-reference model (<see cref="VeinComplexType"/>): the three shapes
/// (concrete | bare parameter | constructed instance), capture-free substitution, equality and rendering.
/// </summary>
public class GenericTypeModelTest : TestContext
{
    private static VeinModuleBuilder CreateModule(string name = "gen_model_test")
        => new(new ModuleNameSymbol(name), Types.Storage);

    private static ClassBuilder DefineGenericClass(VeinModuleBuilder module, string name, params string[] typeParams)
    {
        var @class = module.DefineClass(new NameSymbol(name), NamespaceSymbol.Internal);
        @class.Flags = ClassFlags.Public;
        foreach (var p in typeParams)
            @class.TypeArgs.Add(new VeinTypeArg(p));
        return @class;
    }

    private static VeinClass I4 => VeinTypeCode.TYPE_I4.AsClass()(Types.Storage);
    private static VeinClass Str => VeinTypeCode.TYPE_STRING.AsClass()(Types.Storage);
    private static VeinComplexType Param(string name) => new VeinTypeArg(name);
    private static VeinComplexType Inst(VeinClass def, params VeinComplexType[] args)
        => VeinComplexType.Instance(def, new List<VeinComplexType>(args));

    [Test]
    public void Concrete_ShapeFlags()
    {
        VeinComplexType t = I4;
        Assert.IsFalse(t.IsGeneric);
        Assert.IsFalse(t.IsGenericInstance);
        Assert.IsFalse(t.ContainsGenericParameter);
        Assert.AreEqual(I4.FullName, t.Class.FullName);
    }

    [Test]
    public void Param_ShapeFlags()
    {
        var t = Param("T");
        Assert.IsTrue(t.IsGeneric);
        Assert.IsFalse(t.IsGenericInstance);
        Assert.IsTrue(t.ContainsGenericParameter);
        Assert.AreEqual("T", t.TypeArg.Name);
    }

    [Test]
    public void Instance_ClosedShape()
    {
        var module = CreateModule();
        var listDef = DefineGenericClass(module, "List<T>", "T");
        var inst = Inst(listDef, I4);

        Assert.IsTrue(inst.IsGenericInstance);
        Assert.IsFalse(inst.IsGeneric);
        Assert.IsFalse(inst.ContainsGenericParameter); // closed: List<i32>
        Assert.AreEqual(listDef.FullName, inst.Definition.FullName);
        Assert.AreEqual(listDef.FullName, inst.Class.FullName); // Class falls back to the open definition
        Assert.AreEqual(1, inst.TypeArguments.Count);
        Assert.AreEqual(I4.FullName, inst.TypeArguments[0].Class.FullName);
    }

    [Test]
    public void Instance_OpenMentionsParameter()
    {
        var module = CreateModule();
        var arrayDef = DefineGenericClass(module, "Array<T>", "T");
        var openInst = Inst(arrayDef, Param("T")); // Array<T>
        Assert.IsTrue(openInst.IsGenericInstance);
        Assert.IsTrue(openInst.ContainsGenericParameter);
    }

    [Test]
    public void Substitute_BareParameter()
    {
        var result = Param("T").Substitute(new Dictionary<string, VeinComplexType> { ["T"] = I4 });
        Assert.IsFalse(result.IsGeneric);
        Assert.AreEqual(I4.FullName, result.Class.FullName);
    }

    [Test]
    public void Substitute_ClosesOpenInstance()
    {
        var module = CreateModule();
        var arrayDef = DefineGenericClass(module, "Array<T>", "T");
        var closed = Inst(arrayDef, Param("T"))
            .Substitute(new Dictionary<string, VeinComplexType> { ["T"] = I4 }); // Array<T> -> Array<i32>

        Assert.IsTrue(closed.IsGenericInstance);
        Assert.IsFalse(closed.ContainsGenericParameter);
        Assert.AreEqual(I4.FullName, closed.TypeArguments[0].Class.FullName);
    }

    [Test]
    public void Substitute_RecursesIntoNestedInstance()
    {
        // List<Array<T>>  with  T -> i32  =>  List<Array<i32>>
        var module = CreateModule();
        var listDef = DefineGenericClass(module, "List<T>", "T");
        var arrayDef = DefineGenericClass(module, "Array<T>", "T");

        var nested = Inst(listDef, Inst(arrayDef, Param("T")));
        Assert.IsTrue(nested.ContainsGenericParameter);

        var closed = nested.Substitute(new Dictionary<string, VeinComplexType> { ["T"] = I4 });
        Assert.IsFalse(closed.ContainsGenericParameter);

        var innerArray = closed.TypeArguments[0];
        Assert.IsTrue(innerArray.IsGenericInstance);
        Assert.AreEqual(I4.FullName, innerArray.TypeArguments[0].Class.FullName);
    }

    [Test]
    public void Substitute_ConcreteIsNoOp()
    {
        VeinComplexType concrete = I4;
        var result = concrete.Substitute(new Dictionary<string, VeinComplexType> { ["T"] = Str });
        Assert.AreEqual(I4.FullName, result.Class.FullName);
    }

    [Test]
    public void Substitute_UnmappedParameterUnchanged()
    {
        var result = Param("U").Substitute(new Dictionary<string, VeinComplexType> { ["T"] = I4 });
        Assert.IsTrue(result.IsGeneric);
        Assert.AreEqual("U", result.TypeArg.Name);
    }

    [Test]
    public void Equality_Instances()
    {
        var module = CreateModule();
        var listDef = DefineGenericClass(module, "List<T>", "T");

        var a = Inst(listDef, I4);
        var b = Inst(listDef, I4);
        var c = Inst(listDef, Str);

        Assert.AreEqual(a, b);
        Assert.AreEqual(a.GetHashCode(), b.GetHashCode());
        Assert.AreNotEqual(a, c);
    }

    [Test]
    public void Equality_ShapeMismatch()
    {
        var module = CreateModule();
        var listDef = DefineGenericClass(module, "List<T>", "T");

        VeinComplexType concrete = I4;
        VeinComplexType param = Param("T");
        var inst = Inst(listDef, I4);

        Assert.AreNotEqual(concrete, param);
        Assert.AreNotEqual(concrete, inst);
        Assert.AreNotEqual(param, inst);
    }

    [Test]
    public void Render_InstanceLooksLikeGeneric()
    {
        var module = CreateModule();
        var listDef = DefineGenericClass(module, "List<T>", "T");
        var inst = Inst(listDef, I4);

        var rendered = inst.ToTemplateString();
        StringAssert.Contains("<", rendered);
        StringAssert.Contains(">", rendered);
        // The mangled "<T>" suffix of the definition must not leak as a double-bracket.
        Assert.IsFalse(rendered.Contains("<T>"), $"definition mangle leaked into '{rendered}'");
    }
}
