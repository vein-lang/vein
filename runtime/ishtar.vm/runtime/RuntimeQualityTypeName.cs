namespace ishtar.runtime;

using System.Linq;
using vein.extensions;
using vein.runtime;

// remark: temporary using dotnet api and lazy interning
public unsafe struct RuntimeQualityTypeName(InternedString* fullName)
{
    private readonly InternedString* _fullname = fullName;
    private InternedString* _name;
    private InternedString* _namespace;
    private InternedString* _asmName;
    private InternedString* _nameWithNS;
    
    public bool Equals(RuntimeQualityTypeName* r) => _fullname->Equals(r->_fullname);

    public string Name
    {
        get
        {
            var fn = StringStorage.GetStringUnsafe(fullName);

            if (_name is not null)
                return StringStorage.GetStringUnsafe(_name);
            _name = StringStorage.Intern(fn.Split('/').Last());
            return StringStorage.GetStringUnsafe(_name);
        }
    }


    public string Namespace
    {
        get
        {
            if (_namespace is not null)
                return StringStorage.GetStringUnsafe(_namespace);
            var fn = StringStorage.GetStringUnsafe(fullName);
            _namespace = StringStorage.Intern(fn
                .Split('/')
                .SkipLast(1).Join("/")
                .Split("%").Skip(1)
                .Join("/"));
            return StringStorage.GetStringUnsafe(_namespace);
        }
    }

    public string AssemblyName
    {
        get
        {
            if (_asmName is not null)
                return StringStorage.GetStringUnsafe(_asmName);
            var fn = StringStorage.GetStringUnsafe(fullName);

            _asmName = StringStorage.Intern(fn.Split("%").SkipLast(1).Join());
            return StringStorage.GetStringUnsafe(_asmName);
        }
    }

    public string NameWithNS
    {
        get
        {
            if (_nameWithNS is not null)
                return StringStorage.GetStringUnsafe(_nameWithNS);
            var fn = StringStorage.GetStringUnsafe(fullName);

            _nameWithNS = StringStorage.Intern(fn.Split("%").Skip(1).Join());
            return StringStorage.GetStringUnsafe(_nameWithNS);
        }
    }

    public override string ToString() => StringStorage.GetStringUnsafe(fullName);
}


public static unsafe class RuntimeQualityTypeNameEx
{
    public static RuntimeQualityTypeName* L(this string str)
    {
        var tp = IshtarGC.AllocateImmortal<RuntimeQualityTypeName>();

        var raw = StringStorage.Intern(str);

        *tp = new RuntimeQualityTypeName(raw);

        return tp;
    }

    public static QualityTypeName T(this RuntimeQualityTypeName t)
        => new(t.AssemblyName, t.Name, t.Namespace);

    public static RuntimeQualityTypeName* T(this QualityTypeName t)
    {
        var name = IshtarGC.AllocateImmortal<RuntimeQualityTypeName>();

        *name = new RuntimeQualityTypeName(StringStorage.Intern(t.FullName));

        return name;
    }
}
