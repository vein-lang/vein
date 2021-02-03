namespace wave.emit
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Security;
    using System.Security.Cryptography;
    using System.Text;
    using extensions;

    public class ModuleBuilder : IBaker
    {
        private readonly string _name;
        private readonly Dictionary<int, string> strings = new();
        private readonly List<ClassBuilder> classList = new ();

        public ModuleBuilder(string name) => _name = name;


        public ClassBuilder DefineClass(string name, string @namespace)
        {
            var c = new ClassBuilder(this, name, @namespace);
            classList.Add(c);
            return c;
        }

        public int GetStringConstant(string str)
        {
            if (str == null)
                throw new ArgumentNullException(nameof (str));
            var key = getHashCode(str);
            if (!strings.ContainsKey(key))
                strings[key] = str;
            if (strings[key] != str)
                throw new Exception($"Detected collisions of string constant. '{str}' and '{strings[key]}'.\n " +
                $"Please report this issue into https://github.com/0xF6/wave_lang/issues.");
            return key;
        }

        public byte[] BakeByteArray()
        {
            using var mem = new MemoryStream();
            using var binary = new BinaryWriter(mem);

            var idx = GetStringConstant(_name);
            var name = Encoding.UTF8.GetBytes(_name);
            
            binary.Write(idx);
            binary.Write(name.Length);
            binary.Write(name);
            foreach (var (key, value) in strings)
            {
                binary.Write(key);
                binary.Write(value.Length);
                binary.Write(value);
            }

            foreach (var clazz in classList)
            {
                var body = clazz.BakeByteArray();
                binary.Write(body.Length);
                binary.Write(body);
            }

            return mem.ToArray();
        }

        public string BakeDebugString()
        {
            var str = new StringBuilder();
            str.AppendLine($".module {_name}");
            str.AppendLine("{");
            foreach (var (key, value) in strings) 
                str.AppendLine($"\t.string 0x{key:X8}.'{value}'");
            foreach (var clazz in classList.Select(x => x.BakeDebugString()))
                str.AppendLine($"{clazz.Split('\n').Select(x => $"\t{x}").Join('\n')}");
            str.AppendLine("}");

            return str.ToString();
        }
        
        
        [SecurityCritical]
        private static unsafe int getHashCode(string str)
        {
            fixed (char* chPtr1 = str)
            {
                var num1 = 0x1505; 
                var num2 = num1;
                var chPtr2 = chPtr1;
                int num3;
                while ((num3 = *chPtr2) != 0)
                {
                    num1 = (num1 << 5) + num1 ^ num3;
                    var num4 = (int) chPtr2[1];
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

        public int GetMethodToken(WaveClassMethod method)
        {
            throw new NotImplementedException();
        }
    }
}