namespace ishtar.runtime;

using System.Diagnostics;
using vein.extensions;

[DebuggerDisplay("{Class}.{Name}")]
public unsafe struct RuntimeFieldName(InternedString* fullName)
{
    private InternedString* _name;
    private InternedString* _className;


    public string Name
    {
        get
        {
            var fn = StringStorage.GetStringUnsafe(fullName);

            if (_name is not null)
                return StringStorage.GetStringUnsafe(_name);
            _name = StringStorage.Intern(fn.Split('.').Last());
            return StringStorage.GetStringUnsafe(_name);
        }
    }

    public string Class
    {
        get
        {
            var fn = StringStorage.GetStringUnsafe(fullName);

            if (_name is not null)
                return StringStorage.GetStringUnsafe(_name);
            _name = StringStorage.Intern(fn.Split('.').SkipLast(1).Join());
            return StringStorage.GetStringUnsafe(_name);
        }
    }
        
    public RuntimeFieldName(string name, string className) : this(StringStorage.Intern($"{className}.{name}")) { }
        
    public static RuntimeFieldName* Resolve(int index, RuntimeIshtarModule* module)
    {
        if (!module->fields_table->TryGetValue(index, out var value))
            throw new Exception($"FieldName by index '{index}' not found in '{module->Name}' module.");

        return value;
    }

    public override string ToString() => $"{Class}.{Name}";
}
