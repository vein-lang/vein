namespace veinc_test.Features;

[TestFixture]
public class StructFeatureTest : TestContext
{
    #region Helpers

    private static VeinModuleBuilder CreateModule(string name = "test")
        => new(new ModuleNameSymbol(name), Types.Storage);

    private static ClassBuilder DefineStruct(VeinModuleBuilder module, string name, params (string fieldName, VeinTypeCode type)[] fields)
    {
        var @class = module.DefineClass(new NameSymbol(name), NamespaceSymbol.Internal);
        @class.Flags = ClassFlags.Public | ClassFlags.Struct;
        var offset = 0;
        foreach (var (fieldName, type) in fields)
        {
            var fieldClass = type.AsClass()(Types.Storage);
            var size = type.GetNativeSize();
            var f = @class.DefineField(fieldName, FieldFlags.Public, fieldClass);
            f.Offset = offset;
            f.Size = size;
            offset += size;
        }
        @class.LayoutKind = VeinStructLayoutKind.Sequential;
        @class.StructSize = offset;
        return @class;
    }

    private static ClassBuilder DefineStruct(VeinModuleBuilder module, string name, params (string fieldName, VeinClass type)[] fields)
    {
        var @class = module.DefineClass(new NameSymbol(name), NamespaceSymbol.Internal);
        @class.Flags = ClassFlags.Public | ClassFlags.Struct;
        foreach (var (fieldName, type) in fields)
            @class.DefineField(fieldName, FieldFlags.Public, type);
        @class.LayoutKind = VeinStructLayoutKind.Sequential;
        return @class;
    }

    private static ILGenerator CreateGenerator(params VeinArgumentRef[] args)
    {
        var module = new VeinModuleBuilder(new ModuleNameSymbol(Guid.NewGuid().ToString()), Types.Storage);
        var @class = new ClassBuilder(module, new QualityTypeName(new NameSymbol("bar"), NamespaceSymbol.Internal, module.Name));
        var method = @class.DefineMethod("foo", VeinTypeCode.TYPE_VOID.AsClass()(Types.Storage), args);
        return method.GetGenerator();
    }

    #endregion

    #region Parsing

    [Test]
    public void ParseStructDeclaration()
    {
        var cd = Syntax.ClassDeclaration.ParseVein("public struct Point { x: Int32; y: Int32; }");
        Assert.IsTrue(cd.IsStruct);
        Assert.AreEqual("Point", cd.Identifier.ToString());
        Assert.AreEqual(2, cd.Fields.Count);
    }

    [Test]
    public void ParseStructWithMethods()
    {
        var cd = Syntax.ClassDeclaration.ParseVein("public struct Vector3 { x: Float; y: Float; z: Float; length(): Float { return 0f; } }");
        Assert.IsTrue(cd.IsStruct);
        Assert.AreEqual(3, cd.Fields.Count);
        Assert.AreEqual(1, cd.Methods.Count);
    }

    [Test]
    public void StructIsNotInterface()
    {
        var cd = Syntax.ClassDeclaration.ParseVein("public struct Foo { }");
        Assert.IsTrue(cd.IsStruct);
        Assert.IsFalse(cd.IsInterface);
    }

    [Test]
    public void StructWithInheritance()
    {
        var cd = Syntax.ClassDeclaration.ParseVein("public struct BigInt : ValueType { }");
        Assert.IsTrue(cd.IsStruct);
        Assert.AreEqual(1, cd.Inheritances.Count);
        Assert.AreEqual("ValueType", cd.Inheritances[0].Identifier.ToString());
    }

    #endregion

    #region Flags & Layout

    [Test]
    public void ClassFlagsContainsStruct()
    {
        var flags = ClassFlags.Public | ClassFlags.Struct;
        Assert.IsTrue(flags.HasFlag(ClassFlags.Struct));
    }

    [Test]
    public void StructLayoutKindValues()
    {
        Assert.AreEqual(0, (byte)VeinStructLayoutKind.Auto);
        Assert.AreEqual(1, (byte)VeinStructLayoutKind.Sequential);
        Assert.AreEqual(2, (byte)VeinStructLayoutKind.Explicit);
    }

    [Test]
    public void FieldFlags_HasExplicitOffset()
    {
        var flags = FieldFlags.Public | FieldFlags.HasExplicitOffset;
        Assert.IsTrue(flags.HasFlag(FieldFlags.HasExplicitOffset));
    }

    [Test]
    public void StructClassBuilder_SetsFlags()
    {
        var module = CreateModule();
        var s = DefineStruct(module, "Point", ("x", VeinTypeCode.TYPE_I4), ("y", VeinTypeCode.TYPE_I4));
        Assert.IsTrue(s.IsStruct);
        Assert.IsTrue(s.IsValueType);
    }

    [Test]
    public void StructClassBuilder_FieldOffsets()
    {
        var module = CreateModule();
        var s = DefineStruct(module, "Point", ("x", VeinTypeCode.TYPE_I4), ("y", VeinTypeCode.TYPE_I4));

        Assert.AreEqual(0, s.Fields[0].Offset);
        Assert.AreEqual(4, s.Fields[0].Size);
        Assert.AreEqual(4, s.Fields[1].Offset);
        Assert.AreEqual(4, s.Fields[1].Size);
        Assert.AreEqual(8, s.StructSize);
    }

    [Test]
    public void StructClassBuilder_LayoutProperties()
    {
        var module = CreateModule();
        var s = DefineStruct(module, "Vec2", ("x", VeinTypeCode.TYPE_R4), ("y", VeinTypeCode.TYPE_R4));
        Assert.AreEqual(VeinStructLayoutKind.Sequential, s.LayoutKind);
        Assert.AreEqual(8, s.StructSize);
    }

    [Test]
    public void NonStructClass_IsNotStruct()
    {
        var module = CreateModule();
        var @class = module.DefineClass(new NameSymbol("Bar"), NamespaceSymbol.Internal);
        @class.Flags = ClassFlags.Public;
        Assert.IsFalse(@class.IsStruct);
    }

    #endregion

    #region Bittable

    [Test]
    public void BittableStruct_AllPrimitiveFields()
    {
        var module = CreateModule();
        var s = DefineStruct(module, "Point", ("x", VeinTypeCode.TYPE_I4), ("y", VeinTypeCode.TYPE_I4));
        Assert.IsTrue(s.IsBittable);
    }

    [Test]
    public void BittableStruct_NestedBittableStruct()
    {
        var module = CreateModule();

        var inner = DefineStruct(module, "Point2D", ("x", VeinTypeCode.TYPE_I4), ("y", VeinTypeCode.TYPE_I4));
        Assert.IsTrue(inner.IsBittable);

        var outer = DefineStruct(module, "Rect", ("topLeft", inner), ("bottomRight", inner));
        Assert.IsTrue(outer.IsBittable);
    }

    [Test]
    public void NonBittableStruct_HasReferenceField()
    {
        var module = CreateModule();
        var strType = VeinTypeCode.TYPE_STRING.AsClass()(Types.Storage);
        var i4 = VeinTypeCode.TYPE_I4.AsClass()(Types.Storage);

        var s = DefineStruct(module, "NamedPoint", ("x", i4), ("name", strType));
        Assert.IsFalse(s.IsBittable);
    }

    [Test]
    public void NonBittableStruct_NestedNonBittable()
    {
        var module = CreateModule();
        var strType = VeinTypeCode.TYPE_STRING.AsClass()(Types.Storage);
        var i4 = VeinTypeCode.TYPE_I4.AsClass()(Types.Storage);

        var inner = DefineStruct(module, "NamedValue", ("value", i4), ("label", strType));
        var outer = DefineStruct(module, "Container", ("item", inner));

        Assert.IsFalse(inner.IsBittable);
        Assert.IsFalse(outer.IsBittable);
    }

    [Test]
    public void BittableStruct_StaticFieldsIgnored()
    {
        var module = CreateModule();
        var strType = VeinTypeCode.TYPE_STRING.AsClass()(Types.Storage);

        var s = DefineStruct(module, "Counter", ("value", VeinTypeCode.TYPE_I4));
        s.DefineField("_cache", FieldFlags.Static | FieldFlags.Internal, strType);

        Assert.IsTrue(s.IsBittable);
    }

    #endregion

    #region Serialization Roundtrip

    [Test]
    public void StructSerializationRoundtrip()
    {
        var module = CreateModule("test_rt");
        var s = DefineStruct(module, "TestStruct",
            ("x", VeinTypeCode.TYPE_I4), ("y", VeinTypeCode.TYPE_I4), ("z", VeinTypeCode.TYPE_I4));

        var method = s.DefineMethod("dummy", MethodFlags.Public | MethodFlags.Static,
            VeinTypeCode.TYPE_VOID.AsClass()(Types.Storage));
        method.GetGenerator().Emit(OpCodes.RET);

        var baked = ((IBaker)s).BakeByteArray();
        Assert.IsNotEmpty(baked);

        var readerModule = new ModuleReader_TestHelper(Types.Storage, module);
        var deferMethods = new List<(VeinClass, byte[])>();
        var decoded = ModuleReader.DecodeClass(baked, readerModule, deferMethods);

        Assert.IsTrue(decoded.IsStruct);
        Assert.AreEqual(VeinStructLayoutKind.Sequential, decoded.LayoutKind);
        Assert.AreEqual(12, decoded.StructSize);
        Assert.AreEqual(3, decoded.Fields.Count);

        Assert.AreEqual(0, decoded.Fields[0].Offset);
        Assert.AreEqual(4, decoded.Fields[0].Size);
        Assert.AreEqual(4, decoded.Fields[1].Offset);
        Assert.AreEqual(4, decoded.Fields[1].Size);
        Assert.AreEqual(8, decoded.Fields[2].Offset);
        Assert.AreEqual(4, decoded.Fields[2].Size);
    }

    #endregion

    #region Opcodes

    [Test]
    public void StructOpcodes_Exist()
    {
        Assert.AreEqual(0xB8, (int)OpCodes.BOX.Value);
        Assert.AreEqual(0xB9, (int)OpCodes.UNBOX.Value);
        Assert.AreEqual(0xBA, (int)OpCodes.INITSTRUCT.Value);
        Assert.AreEqual(0xBB, (int)OpCodes.CPSTRUCT.Value);
        Assert.AreEqual(0xBC, (int)OpCodes.LDSTRUCT_F.Value);
        Assert.AreEqual(0xBD, (int)OpCodes.STSTRUCT_F.Value);
    }

    [Test]
    public void StructOpcodes_EmitAndDecode()
    {
        var gen = CreateGenerator();

        gen.Emit(OpCodes.INITSTRUCT, 42);
        gen.Emit(OpCodes.LDSTRUCT_F, 1);
        gen.Emit(OpCodes.STSTRUCT_F, 2);
        gen.Emit(OpCodes.CPSTRUCT, 42);
        gen.Emit(OpCodes.BOX, 42);
        gen.Emit(OpCodes.UNBOX, 42);

        var (result, _) = ILReader.Deconstruct(gen.BakeByteArray(), "");

        Assert.AreEqual(OpCodes.INITSTRUCT.Value, result[0]);
        Assert.AreEqual((uint)42, result[1]);
        Assert.AreEqual(OpCodes.LDSTRUCT_F.Value, result[2]);
        Assert.AreEqual((uint)1, result[3]);
        Assert.AreEqual(OpCodes.STSTRUCT_F.Value, result[4]);
        Assert.AreEqual((uint)2, result[5]);
        Assert.AreEqual(OpCodes.CPSTRUCT.Value, result[6]);
        Assert.AreEqual((uint)42, result[7]);
        Assert.AreEqual(OpCodes.BOX.Value, result[8]);
        Assert.AreEqual((uint)42, result[9]);
        Assert.AreEqual(OpCodes.UNBOX.Value, result[10]);
        Assert.AreEqual((uint)42, result[11]);
    }

    #endregion
}

internal class ModuleReader_TestHelper : ModuleReader
{
    public ModuleReader_TestHelper(VeinCore types, VeinModuleBuilder source) : base(types)
    {
        foreach (var (k, v) in source.strings_table)
            strings_table[k] = v;
        foreach (var (k, v) in source.types_table)
            types_table[k] = v;
        foreach (var (k, v) in source.fields_table)
            fields_table[k] = v;
    }
}
