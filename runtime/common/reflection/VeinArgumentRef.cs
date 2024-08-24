namespace vein.runtime;
#nullable enable
using System;
using System.Diagnostics;

[DebuggerDisplay("{ToStringDebug()}")]
public class VeinArgumentRef(string name, VeinComplexType complexType)
{
    public const string THIS_ARGUMENT = "<this>";

    public VeinComplexType ComplexType { get; private set; } = complexType;
    public string Name { get; } = name;


    public void Temp_ReplaceType(VeinClass @class)
    {
        if (IsGeneric) throw new NotSupportedException();
        ComplexType = @class;
    }


    public VeinTypeArg TypeArg => ComplexType.TypeArg;
    public VeinClass Type => ComplexType.Class;


    public bool IsGeneric => ComplexType.IsGeneric;
    public bool IsThis => Name.Equals(THIS_ARGUMENT);

    public string ToTemplateString() => ComplexType!.ToTemplateString();
    public string ToShortTemplateString() => ComplexType!.ToShortTemplateString();


    public static VeinArgumentRef Create(VeinCore types, (string name, VeinTypeCode code) data)
        => new(data.name, data.code.AsClass()(types));

    public static VeinArgumentRef Create(VeinCore types, (VeinTypeCode code, string name) data) =>
        Create(types, (data.name, data.code));

    public static VeinArgumentRef CreateThis(VeinClass clazz) => new(THIS_ARGUMENT, clazz);


    private string ToStringDebug() => $"Argument ({Name}: {ComplexType})";
}
