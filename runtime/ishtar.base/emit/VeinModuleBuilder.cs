namespace ishtar.emit;

using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using extensions;
using ishtar;
using vein;
using vein.exceptions;
using vein.extensions;
using vein.runtime;

public class VeinModuleBuilder : VeinModule, IBaker
{
    public VeinModuleBuilder(ModuleNameSymbol name, VeinCore types) : base(name, types) { }
    public VeinModuleBuilder(ModuleNameSymbol name, Version ver, VeinCore types) : base(name, ver, types) { }

    /// <summary>
    /// Define class by name.
    /// </summary>
    /// <remarks>
    /// 'assemblyName%namespace/className' - VALID
    /// <br/>
    /// 'namespace/className' - VALID
    /// <br/>
    /// 'namespace/className' - INVALID, need '' prefix.
    /// <br/>
    /// 'className' - INVALID, need describe namespace.
    /// </remarks>
    public ClassBuilder DefineClass(NameSymbol className, NamespaceSymbol @namespace)
        => DefineClass(new QualityTypeName(className, @namespace, Name));

    /// <summary>
    /// Define class by name.
    /// </summary>
    public ClassBuilder DefineClass(QualityTypeName name)
    {
        if (class_table.Any(x => x.FullName.Equals(name)))
            throw new DuplicateNameException($"Class '{name}' already defined.");
        InternString(name.Name.name);
        InternString(name.Namespace.@namespace);
        InternString(name.ModuleName.moduleName);
        var c = new ClassBuilder(this, name);
        class_table.Add(c);
        return c;
    }

    /// <summary>
    /// Define class by name.
    /// </summary>
    public ClassBuilder DefineClass(QualityTypeName name, VeinClass parent)
    {
        if (class_table.Any(x => x.FullName.Equals(name)))
            throw new DuplicateNameException($"Class '{name}' already defined.");
        InternString(name.Name.name);
        InternString(name.Namespace.@namespace);
        InternString(name.ModuleName.moduleName);
        var c = new ClassBuilder(this, name, parent);
        class_table.Add(c);
        return c;
    }

    private int _intern<T>(Dictionary<int, T> storage, T val)
    {
        var (key, value) = storage.FirstOrDefault(x => x.Value.Equals(val));

        if (value is not null)
            return key;

        storage[key = storage.Count] = val;
        return key;
    }

    /// <summary>
    /// Intern string constant into module storage and return string index.
    /// </summary>
    /// <exception cref="ArgumentNullException"></exception>
    public int InternString(string str)
    {
        if (str == null)
            throw new ArgumentNullException(nameof(str));
        var key = _intern(strings_table, str);


        //logger.Information("String constant '{str}' baked by index: {key}", str, key);
        return key;
    }
    /// <summary>
    /// Intern TypeName constant into module storage and return TypeName index.
    /// </summary>
    /// <exception cref="ArgumentNullException"></exception>
    public int InternTypeName(QualityTypeName name)
    {
        if (name == null)
            throw new ArgumentNullException(nameof(name));
        var key = _intern(types_table, name);

        //logger.Information("TypeName '{name}' baked by index: {key}", name, key);
        return key;
    }

    /// <summary>
    /// Intern TypeName constant into module storage and return TypeName index.
    /// </summary>
    /// <exception cref="ArgumentNullException"></exception>
    public int InternGenericTypeName(VeinTypeArg name)
    {
        if (name == null)
            throw new ArgumentNullException(nameof(name));
        var key = _intern(generics_table, name);

        //logger.Information("TypeName '{name}' baked by index: {key}", name, key);
        return key;
    }
    /// <summary>
    /// Intern FieldName constant into module storage and return FieldName index.
    /// </summary>
    /// <exception cref="ArgumentNullException"></exception>
    public int InternFieldName(FieldName name)
    {
        if (name == null)
            throw new ArgumentNullException(nameof(name));

        var key = _intern(fields_table, name);

        //logger.Information("FieldName '{name}' baked by index: {key}", name, key);
        return key;
    }

    internal (int, QualityTypeName) GetMethodToken(VeinMethod method) =>
        (this.InternString(method.Name), method.Owner.FullName);
    /// <summary>
    /// Bake result into il byte code.
    /// </summary>
    public byte[] BakeByteArray()
    {
        class_table.OfType<IBaker>().ForEach(x => x.BakeDebugString());
        class_table.OfType<IBaker>().ForEach(x => x.BakeByteArray());

        using var mem = new MemoryStream();
        using var binary = new BinaryWriter(mem);

        var idx = InternString(Name.moduleName);
        var vdx = InternString(Version.ToString());

        binary.Write(idx);
        binary.Write(vdx);
        binary.Write(OpCodes.SetVersion);

        binary.Write(strings_table.Count);
        foreach (var (key, value) in strings_table)
        {
            binary.Write(key);
            binary.WriteIshtarString(value);
        }
        binary.Write(types_table.Count);
        foreach (var (key, value) in types_table)
        {
            binary.Write(key);
            binary.WriteIshtarString(value.ModuleName.moduleName);
            binary.WriteIshtarString(value.Namespace.@namespace);
            binary.WriteIshtarString(value.Name.name);
        }
        binary.Write(fields_table.Count);
        foreach (var (key, value) in fields_table)
        {
            binary.Write(key);
            binary.WriteIshtarString(value.Name);
            binary.WriteIshtarString(value.Class);
        }

        binary.Write(Deps.Count);
        foreach (var dep in Deps)
        {
            binary.WriteIshtarString(dep.Name.moduleName);
            binary.WriteIshtarString(dep.Version.ToString());
        }

        binary.Write(class_table.Count);
        foreach (var clazz in class_table.OfType<IBaker>())
        {
            var body = clazz.BakeByteArray();
            binary.Write(body.Length);
            binary.Write(body);
        }

        binary.Write(alias_table.Count);
        foreach (var (alias, i) in alias_table.Select((x, y) => (x,y)))
        {
            binary.Write(i);
            binary.WriteTypeName(alias.aliasName, this);

            switch (alias)
            {
                case VeinAliasType t:
                    binary.Write(true);
                    binary.WriteTypeName(t.type.FullName, this);
                    break;
                case VeinAliasMethod method:
                    binary.Write(false);
                    binary.WriteComplexType(method.method.ReturnType, this);
                    binary.WriteArguments(method.method, this);
                    break;
                default:
                    throw new NotSupportedException();
            }
        }

        binary.Write(generics_table.Count);
        foreach (var (i, generic) in generics_table)
        {
            binary.Write(i);
            binary.WriteIshtarString(generic.Name);

            binary.Write(generic.Constraints.Count);
            foreach (var constraint in generic.Constraints)
            {
                switch (constraint.Constraint)
                {
                    case VeinTypeParameterConstraint.BITTABLE:
                    case VeinTypeParameterConstraint.CLASS:
                        break;
                    case VeinTypeParameterConstraint.TYPE when constraint is VeinBaseConstraintConstType t:
                        binary.WriteTypeName(t.classes.FullName, this);
                        break;
                    case VeinTypeParameterConstraint.SIGNATURE when constraint is VeinBaseConstraintConstSignature s:
                        binary.WriteTypeName(s.@interface.FullName, this);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        var constBody = const_table.BakeByteArray();
        binary.Write(constBody.Length);
        binary.Write(constBody);

        return mem.ToArray();
    }
    /// <summary>
    /// Bake result into debug il preview document.
    /// </summary>
    public string BakeDebugString()
    {
        var str = new StringBuilder();
        str.AppendLine($".module '{Name.moduleName}'::'{Version}'");
        str.AppendLine("{");

        foreach (var value in alias_table.OfType<VeinAliasType>())
            str.AppendLine($"\t.alias '{value.aliasName}' -> '{value.type.FullName}'");
        foreach (var value in alias_table.OfType<VeinAliasMethod>())
            str.AppendLine($"\t.alias '{value.aliasName}' -> '{value.method.ToTemplateString()}'");

        foreach (var dep in Deps)
            str.AppendLine($"\t.dep '{dep.Name}'::'{dep.Version}'");

        str.AppendLine("\n\t.table const");
        str.AppendLine("\t{");
        foreach (var (key, value) in strings_table)
            str.AppendLine($"\t\t.s {key:D6}:'{value}'");

        foreach (var (key, value) in types_table)
            str.AppendLine($"\t\t.t {key:D6}:'{value}'");

        foreach (var (key, value) in fields_table)
            str.AppendLine($"\t\t.f {key:D6}:'{value}'");
        str.AppendLine("\t}\n");


        str.AppendLine(const_table.BakeDebugString().Split('\n').Select(x => $"\t{x}").Join('\n'));

        foreach (var clazz in class_table.OfType<IBaker>().Select(x => x.BakeDebugString()))
            str.AppendLine($"{clazz.Split('\n').Select(x => $"\t{x}").Join('\n')}");
        str.AppendLine("}");

        return str.ToString();
    }

    public string BakeDiagnosticDebugString() => throw new NotImplementedException();
}
