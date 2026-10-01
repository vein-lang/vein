namespace vein.runtime;

using System;
using System.Collections.Generic;
using System.Linq;
using extensions;

/// <summary>
/// A reference to a type that may be one of three shapes:
/// <list type="bullet">
/// <item>concrete — a closed <see cref="VeinClass"/> such as <c>i32</c> or <c>string</c>;</item>
/// <item>a bare generic parameter — a <see cref="VeinTypeArg"/> such as <c>T</c>;</item>
/// <item>a constructed generic instance — an open definition plus type arguments,
/// such as <c>List&lt;int&gt;</c> or <c>Array&lt;T&gt;</c>.</item>
/// </list>
/// The instance case carries the full argument tree, so nested / "transit" generics
/// (<c>List&lt;List&lt;T&gt;&gt;</c>, or a generic body that uses <c>Array&lt;T&gt;</c>)
/// are representable directly instead of being mangled into the class-name string.
/// </summary>
public record VeinComplexType
{
    private readonly VeinTypeArg _typeArg;
    private readonly VeinClass _class;
    private readonly VeinClass _instanceDef;
    private readonly IReadOnlyList<VeinComplexType> _instanceArgs;

    public VeinComplexType(VeinClass @class) => _class = @class;

    public VeinComplexType(VeinTypeArg typeArg) => _typeArg = typeArg;

    public VeinComplexType(VeinClass definition, IReadOnlyList<VeinComplexType> typeArguments)
    {
        _instanceDef = definition;
        _instanceArgs = typeArguments;
    }

    /// <summary>True when this is a bare generic parameter (e.g. <c>T</c>).</summary>
    public bool IsGeneric => _typeArg is not null;

    /// <summary>True when this is a constructed generic type (e.g. <c>List&lt;int&gt;</c>, <c>Array&lt;T&gt;</c>).</summary>
    public bool IsGenericInstance => _instanceDef is not null;

    /// <summary>True when this type, recursively, mentions any bare generic parameter (i.e. it is open).</summary>
    public bool ContainsGenericParameter =>
        IsGeneric || (IsGenericInstance && _instanceArgs.Any(a => a.ContainsGenericParameter));

    public VeinTypeArg TypeArg => _typeArg;

    /// <summary>
    /// The underlying class: the concrete class for a closed type, or the open
    /// definition for a generic instance. Null for a bare generic parameter.
    /// </summary>
    public VeinClass Class => _class ?? _instanceDef;

    /// <summary>The open generic definition when <see cref="IsGenericInstance"/>; otherwise null.</summary>
    public VeinClass Definition => _instanceDef;

    /// <summary>The type arguments when <see cref="IsGenericInstance"/>; otherwise empty.</summary>
    public IReadOnlyList<VeinComplexType> TypeArguments => _instanceArgs ?? Array.Empty<VeinComplexType>();

    /// <summary>Construct a generic instance type reference (e.g. <c>List&lt;int&gt;</c>).</summary>
    public static VeinComplexType Instance(VeinClass definition, IReadOnlyList<VeinComplexType> args)
        => new(definition, args);

    public static implicit operator VeinComplexType(VeinClass cpx)
        => cpx is null ? null : new VeinComplexType(cpx);

    public static implicit operator VeinComplexType(VeinTypeArg cpx)
        => cpx is null ? null : new VeinComplexType(cpx);

    public static implicit operator VeinClass(VeinComplexType cpx)
    {
        if (cpx is null) return null;
        if (cpx.IsGeneric)
            throw new NotSupportedException(
                $"Trying summon non generic vein type, but complex type is generic parameter '{cpx._typeArg}'");
        // For a constructed instance this yields the open definition (no type loss vs. the old model).
        return cpx.Class;
    }

    public static implicit operator VeinTypeArg(VeinComplexType cpx)
    {
        if (cpx is null) return null;
        if (!cpx.IsGeneric)
            throw new NotSupportedException(
                $"Trying summon generic type, but complex type is non generic vein type");
        return cpx._typeArg;
    }

    /// <summary>
    /// Capture-free substitution of bare generic parameters by name. Recurses into
    /// instance arguments, so <c>Array&lt;T&gt;</c> with <c>T -&gt; int</c> becomes
    /// <c>Array&lt;int&gt;</c> and nested generics substitute correctly.
    /// </summary>
    public VeinComplexType Substitute(IReadOnlyDictionary<string, VeinComplexType> substitution)
    {
        if (IsGeneric)
            return substitution.TryGetValue(_typeArg.Name, out var replacement) ? replacement : this;
        if (IsGenericInstance)
            return new VeinComplexType(_instanceDef,
                _instanceArgs.Select(a => a.Substitute(substitution)).ToList());
        return this;
    }

    // The open definition's name is still mangled with its parameter (e.g. "Span<T>"); strip that
    // suffix so a constructed instance renders with its real type arguments, e.g.
    // "[std]::std::Span<Byte>" — which is what method-signature keys (including native FFI bindings)
    // expect. (Full de-mangling of definition names is a separate follow-up.)
    private static string StripGenericSuffix(string name)
    {
        var idx = name.IndexOf('<');
        return idx < 0 ? name : name.Substring(0, idx);
    }

    public string ToTemplateString() =>
        IsGeneric ? _typeArg.ToTemplateString() :
        IsGenericInstance ? $"{StripGenericSuffix(_instanceDef.FullName.ToString())}<{_instanceArgs.Select(a => a.ToShortTemplateString()).Join(',')}>" :
        _class.FullName.ToString();

    public string ToShortTemplateString() =>
        IsGeneric ? _typeArg.ToTemplateString() :
        IsGenericInstance ? $"{StripGenericSuffix(_instanceDef.Name.ToString())}<{_instanceArgs.Select(a => a.ToShortTemplateString()).Join(',')}>" :
        _class.Name.ToString();

    public override string ToString() => ToTemplateString();

    public virtual bool Equals(VeinComplexType other)
    {
        if (other is null) return false;
        if (IsGeneric != other.IsGeneric) return false;
        if (IsGenericInstance != other.IsGenericInstance) return false;
        if (IsGeneric)
            return Equals(_typeArg, other._typeArg);
        if (IsGenericInstance)
            return _instanceDef.FullName.Equals(other._instanceDef.FullName)
                   && _instanceArgs.Count == other._instanceArgs.Count
                   && _instanceArgs.SequenceEqual(other._instanceArgs);
        return Class.FullName.Equals(other.Class.FullName);
    }

    public override int GetHashCode()
    {
        if (IsGeneric)
            return _typeArg.GetHashCode();
        if (IsGenericInstance)
        {
            var hash = _instanceDef.FullName.GetHashCode();
            foreach (var a in _instanceArgs)
                hash = HashCode.Combine(hash, a.GetHashCode());
            return hash;
        }
        return Class.FullName.GetHashCode();
    }
}
