namespace vein.runtime;

using System;
using System.Linq;
using System.Text.RegularExpressions;
using extensions;

public class InvalidTypeNameException(string msg) : Exception(msg);
public class QualityTypeName(string fullName) : IEquatable<QualityTypeName>
{
    private string _name, _namespace, _asmName, _nameWithNS;
    private readonly string _fullName = fullName;

    public string Name
    {
        get
        {
            if (_name is not null)
                return _name;
            return _name = _fullName.Split('/').Last();
        }
    }

    public string Namespace
    {
        get
        {
            if (_namespace is not null)
                return _namespace;
            return _namespace = _fullName
                .Split('/')
                .SkipLast(1).Join("/")
                .Split("%").Skip(1)
                .Join("/");
        }
    }

    public string AssemblyName
    {
        get
        {
            if (_asmName is not null)
                return _asmName;
            return _asmName = _fullName.Split("%").SkipLast(1).Join();
        }
    }

    public string NameWithNS
    {
        get
        {
            if (_nameWithNS is not null)
                return _nameWithNS;
            return _nameWithNS = _fullName.Split("%").Skip(1).Join();
        }
    }

    public string FullName => _fullName;

    public QualityTypeName(string asmName, string name, string ns) : this($"{asmName}%{ns}/{name}")
    {
        _namespace = ns;
        _name = name;
        _asmName = asmName;
        _nameWithNS = $"{ns}/{name}";
    }

    public static implicit operator QualityTypeName(string name)
    {
        if (!Regex.IsMatch(name, @"(.+)\%global::(.+)\/(.+)"))
            throw new InvalidTypeNameException($"'{name}' is not valid type name.");
        return new QualityTypeName(name);
    }

    public override string ToString() => _fullName;




    internal T TryGet<T>(Func<QualityTypeName, T> t) where T : class
    {
        try
        {
            return t(this);
        }
        catch
        {
            return null;
        }
    }

    public static bool operator ==(QualityTypeName q1, QualityTypeName q2)
    {
        if (q1 is null or { _fullName: null }) return false;
        if (q2 is null or { _fullName: null }) return false;
        return q1._fullName.Equals(q2._fullName, StringComparison.InvariantCultureIgnoreCase);
    }

    public static bool operator !=(QualityTypeName q1, QualityTypeName q2) => !(q1 == q2);
    protected bool Equals(QualityTypeName other) => _fullName == other._fullName;

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((QualityTypeName)obj);
    }

    public override int GetHashCode() => (_fullName != null ? _fullName.GetHashCode() : 0);
    bool IEquatable<QualityTypeName>.Equals(QualityTypeName other) => Equals(other);
}
