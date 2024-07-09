namespace ishtar.runtime;

using vein.extensions;

[DebuggerDisplay("{Class}.{Name}")]
public unsafe struct RuntimeFieldName(InternedString* fullName)
{
    private InternedString* _fullName = fullName;
    private InternedString* _name = CreateName(fullName);
    private InternedString* _className = CreateClassName(fullName);

    public bool Equals(RuntimeFieldName* name) => fullName->Equals(name->_fullName);


    private static InternedString* CreateName(InternedString* full)
    {
        var fn = StringStorage.GetStringUnsafe(full);
        return StringStorage.Intern(fn.Split('.').Last(), full);
    }

    private static InternedString* CreateClassName(InternedString* full)
    {
        var fn = StringStorage.GetStringUnsafe(full);
        return StringStorage.Intern(fn.Split('.').SkipLast(1).Join(), full);
    }

    public string Name => StringStorage.GetStringUnsafe(_name);

    public string Class => StringStorage.GetStringUnsafe(_className);
    public string Fullname => StringStorage.GetStringUnsafe(_fullName);

    public RuntimeFieldName(string name, string className, void* parent) : this(StringStorage.Intern($"{className}.{name}", parent)) { }
        
    public static RuntimeFieldName* Resolve(int index, RuntimeIshtarModule* module)
    {
        if (!module->fields_table->TryGetValue(index, out var value))
            throw new Exception($"FieldName by index '{index}' not found in '{module->Name}' module.");

        return value;
    }

    public override string ToString() => $"{Class}.{Name}";
}
