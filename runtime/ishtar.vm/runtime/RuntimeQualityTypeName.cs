namespace ishtar.runtime;

using collections;
using gc;
using vein.runtime;

public readonly unsafe struct RuntimeQualityTypeName : IEq<RuntimeQualityTypeName>
{
    public RuntimeQualityTypeName() => throw new NotSupportedException();


    private readonly InternedString* _name;
    private readonly InternedString* _namespace;
    private readonly InternedString* _moduleName;


    public RuntimeQualityTypeName(InternedString* Name, InternedString* Namespace, InternedString* ModuleName)
    {
        this._name = Name;
        this._namespace = Namespace;
        this._moduleName = ModuleName;
    }

    
    public string Name => StringStorage.GetStringUnsafe(_name);
    public string Namespace => StringStorage.GetStringUnsafe(_namespace);
    public string AssemblyName => StringStorage.GetStringUnsafe(_moduleName);

    public string NameWithNS => $"{Namespace}::{Name}";

    public static bool Eq(RuntimeQualityTypeName* p1, RuntimeQualityTypeName* p2) =>
        InternedString.Eq(p1->_name, p2->_name) &&
        InternedString.Eq(p1->_namespace, p2->_namespace) &&
        InternedString.Eq(p1->_moduleName, p2->_moduleName);

    public override string ToString() => $"[{AssemblyName}]::{Namespace}::{Name}";


    public static RuntimeQualityTypeName* New(string name, string @namespace, string moduleName, void* parent)
    {
        var n = IshtarGC.AllocateImmortal<RuntimeQualityTypeName>(parent);

        *n = new RuntimeQualityTypeName(
            StringStorage.Intern(name, parent),
            StringStorage.Intern(@namespace, parent),
            StringStorage.Intern(moduleName, parent)
        );

        return n;
    }
}


public static unsafe class RuntimeQualityTypeNameEx
{
    public static RuntimeQualityTypeName* T(this QualityTypeName t, void* parent)
    {
        var name = IshtarGC.AllocateImmortal<RuntimeQualityTypeName>(parent);

        *name = new RuntimeQualityTypeName(
            StringStorage.Intern(t.Name.name, name),
            StringStorage.Intern(t.Namespace.@namespace, name),
            StringStorage.Intern(t.ModuleName.moduleName, name)
            );

        return name;
    }
}
