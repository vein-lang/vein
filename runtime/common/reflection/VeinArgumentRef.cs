namespace vein.runtime;
#nullable enable
using System;

public class VeinArgumentRef
{
    public const string THIS_ARGUMENT = "<this>";

    public VeinComplexType ComplexType { get; private set; }
    public string Name { get; }


    public void Temp_ReplaceType(VeinClass @class)
    {
        if (IsGeneric) throw new NotSupportedException();
        ComplexType = @class;
    }


    public VeinTypeArg TypeArg => ComplexType.TypeArg;
    public VeinClass Type => ComplexType.Class;


    public bool IsGeneric => ComplexType.IsGeneric;

    public VeinArgumentRef(string name, VeinClass clazz)
    {
        Name = name;
        ComplexType = clazz;
    }

    public VeinArgumentRef(string name, VeinTypeArg typeArg)
    {
        Name = name;
        ComplexType = typeArg;
    }

    public string ToTemplateString() => ComplexType!.ToTemplateString();


    public static VeinArgumentRef Create(VeinCore types, (string name, VeinTypeCode code) data)
        => new(data.name, data.code.AsClass()(types));

    public static VeinArgumentRef Create(VeinCore types, (VeinTypeCode code, string name) data) =>
        Create(types, (data.name, data.code));
}
