namespace wave.emit
{
    using System;
    using System.Linq;
    using System.Security;
    using System.Text.RegularExpressions;
    [Obsolete]
    public record RuntimeToken(string text, ulong Value)
    {
        public static RuntimeToken Create(string id) 
            => new(id, getHashCode(id));
        [SecurityCritical]
        private static unsafe ulong getHashCode(string str)
        {
            fixed (char* chPtr1 = str)
            {
                var num1 = 0x1505UL; 
                var num2 = num1;
                var chPtr2 = chPtr1;
                ulong num3;
                while ((num3 = *chPtr2) != 0)
                {
                    num1 = (num1 << 5) + num1 ^ num3;
                    var num4 = (ulong) chPtr2[1];
                    if (num4 != 0)
                    {
                        num2 = (num2 << 5) + num2 ^ num4;
                        chPtr2 += 2;
                    }
                    else break;
                }
                return num1 + num2 * 0x5D588B65;
            }
        }
    }
    
    public record QualityTypeName(string fullName)
    {
        public string Name => fullName.Split('/').Last();
        public string Namespace => fullName
            .Split('/')
            .SkipLast(1).Join("/")
            .Split("%").Skip(1)
            .Join("/");

        public string AssemblyName => fullName.Split("%").SkipLast(1).Join();
        
        
        public QualityTypeName(string asmName, string name, string ns) : this($"{asmName}%{ns}/{name}") { }
        
        public static QualityTypeName Construct(in int asmIdx, in int nameIdx, in int namespaceIdx, WaveModule module) 
            => new(
                module.GetConstByIndex(asmIdx), 
                module.GetConstByIndex(nameIdx), 
                module.GetConstByIndex(namespaceIdx));
        
        
        public static implicit operator QualityTypeName(string name)
        {
            if (!Regex.IsMatch(name, @"(.+)\%global::(.+)\/(.+)"))
                throw new Exception($"'{name}' is not valid type name.");
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