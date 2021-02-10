namespace wave.emit
{
    using System.Linq;
    using System.Security;

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



    public record TypeName(string fullName)
    {
        public string Name => fullName.Split('/').Last();
        public string Namespace => fullName.Split('/').SkipLast(1).Join("/");

        public RuntimeToken Token 
            => RuntimeToken.Create(fullName);


        public static implicit operator string(TypeName t) => t.fullName;
        public static implicit operator TypeName(string t) => new(t);


        public TypeName(string name, string ns) : this($"{ns}/{name}") { }

        public static TypeName Construct(in int nameIdx, in int namespaceIdx, WaveModule module) 
            => new(module.GetConstByIndex(nameIdx), module.GetConstByIndex(namespaceIdx));
        public static TypeName Construct(in long fullIdx, WaveModule module) 
            => new(module.GetConstByIndex((int)(fullIdx >> 32)), 
                module.GetConstByIndex((int)(fullIdx & uint.MaxValue)));
    }
}