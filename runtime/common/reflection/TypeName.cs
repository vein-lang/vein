namespace vein.runtime;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using extensions;

public class InvalidTypeNameException(string msg) : Exception(msg);

public record struct NameSymbol(string name)
{
    public static NameSymbol FunctionMulticast = new("FunctionMulticast");
    public static NameSymbol ValueType = new("ValueType");

    public bool HasGenerics => name.ContainsAll("<", ">");
    public bool HasUnderlying { get; private init; }


    public NameSymbol ToUnderlying()
    {
        if (!HasGenerics)
            throw new InvalidOperationException();
        var rex = new Regex(@"(\w+)");
        var all = new Regex(@"\<(.+)\>");

        return new NameSymbol($"{all.Replace(name, "")}<{new string(',', rex.Matches(name).Count - 2)}>")
        {
            HasUnderlying = true
        };
    }

    public bool Equals(NameSymbol other)
        => Equals((NameSymbol?)other);
    
    public override int GetHashCode() => HashCode.Combine(name);

    public bool Equals(NameSymbol? other)
    {
        if (other is null)
            return false;
        if (other.Value.HasGenerics != HasGenerics)
            return false;
        if (HasGenerics)
            return ToUnderlying().name.Equals(other.Value.ToUnderlying().name);
        return other.Value.name.Equals(this.name);
    }

    public static bool operator ==(NameSymbol? n1, NameSymbol? n2)
    {
        if (n1 is null || n2 is null)
            return false;
        return n1.Value.Equals(n2);
    }
    public static bool operator !=(NameSymbol? one, NameSymbol? other)
        => !(one == other);
    public static NameSymbol WithGenerics(string name, List<NameSymbol> generics)
    {
        if (generics.Count == 0)
            return new NameSymbol(name);
        return new NameSymbol($"{name}<{generics.Select(x => new VeinTypeArg(x.name).ToString()).Join(',')}>");
    }
}

public record struct NamespaceSymbol(string @namespace)
{
    public static NamespaceSymbol Std = new("std");
    public static NamespaceSymbol Internal = new("@internal");
}

public record struct ModuleNameSymbol(string moduleName)
{
    public static ModuleNameSymbol Std = new("std");
}

public sealed record QualityTypeName(NameSymbol Name, NamespaceSymbol Namespace, ModuleNameSymbol ModuleName)
{

    public const string NAME_DIVIDER = "::";

    public string NameWithNS => $"{Namespace.@namespace}{NAME_DIVIDER}{Name.name}";

    public string FullName =>
        $"[{ModuleName.moduleName}]{NAME_DIVIDER}{Namespace.@namespace}{NAME_DIVIDER}{Name.name}";

    public override string ToString() => FullName;


    public QualityTypeName OverrideName(NameSymbol name) => this with { Name = name };
}
