namespace mana.runtime
{
    using System;
    using System.Linq;
    using System.Text.RegularExpressions;
    using extensions;

    public class InvalidTypeNameException : Exception { public InvalidTypeNameException(string msg) : base(msg) { } }
    public record QualityTypeName(string fullName)
    {
        public string Name => fullName.Split('/').Last();
        public string Namespace => fullName
            .Split('/')
            .SkipLast(1).Join("/")
            .Split("%").Skip(1)
            .Join("/");

        public string AssemblyName => fullName.Split("%").SkipLast(1).Join();

        public string NameWithNS => fullName.Split("%").Skip(1).Join();

        public QualityTypeName(string asmName, string name, string ns) : this($"{asmName}%{ns}/{name}") { }

        public static implicit operator QualityTypeName(string name)
        {
            if (!Regex.IsMatch(name, @"(.+)\%global::(.+)\/(.+)"))
                throw new InvalidTypeNameException($"'{name}' is not valid type name.");
            return new QualityTypeName(name);
        }

        public override string ToString() => fullName;

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
    }
}
