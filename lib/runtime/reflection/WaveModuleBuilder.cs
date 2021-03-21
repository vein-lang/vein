namespace wave.emit
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Security;
    using System.Text;
    using exceptions;
    using MoreLinq;

    public class WaveModuleBuilder : WaveModule, IBaker
    {
        public WaveModuleBuilder(string name) : base(name) {}

        /// <summary>
        /// Define class by name.
        /// </summary>
        /// <remarks>
        /// 'assemblyName%global::namespace/className' - VALID
        /// <br/>
        /// 'global::namespace/className' - VALID
        /// <br/>
        /// 'namespace/className' - INVALID, need 'global::' prefix.
        /// <br/>
        /// 'className' - INVALID, need describe namespace.
        /// </remarks>
        /// <exception cref="IncompleteClassNameException">See 'remarks'.</exception>
        public ClassBuilder DefineClass(string classFullname)
        {
            if (!classFullname.Contains("/"))
                throw new IncompleteClassNameException("Class name not contained namespace.");
            var typename = default(QualityTypeName);
            if (classFullname.Contains("%"))
            {
                if (!classFullname.StartsWith($"{Name}%"))
                    throw new IncompleteClassNameException($"Class name contains incorrect assembly name.");
                typename = new QualityTypeName(classFullname);
            }
            else
                typename = new QualityTypeName($"{Name}%{classFullname}");

            if (typename.TryGet(x => x.Namespace) is null)
                throw new IncompleteClassNameException($"Class name has incorrect format.");
            if (!typename.Namespace.StartsWith("global::"))
                throw new IncompleteClassNameException($"Class namespace not start with 'global::'.");

            return DefineClass(typename);
        }
        /// <summary>
        /// Define class by name.
        /// </summary>
        public ClassBuilder DefineClass(QualityTypeName name)
        {
            GetStringConstant(name.Name);
            GetStringConstant(name.Namespace);
            var c = new ClassBuilder(this, name);
            classList.Add(c);
            return c;
        }
        /// <summary>
        /// Intern string constant into module storage and return string index.
        /// </summary>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="StringCollisionException"></exception>
        public int GetStringConstant(string str)
        {
            if (str == null)
                throw new ArgumentNullException(nameof (str));
            var key = getHashCode(str);
            if (!strings.ContainsKey(key))
                strings[key] = str;
            if (strings[key] != str)
                throw new StringCollisionException(str, strings[key]);
            return key;
        }
        /// <summary>
        /// Intern TypeName constant into module storage and return TypeName index.
        /// </summary>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="StringCollisionException"></exception>
        public long GetTypeConstant(QualityTypeName name)
        {
            var i1 = GetStringConstant(name.Namespace);
            var i2 = GetStringConstant(name.Name);
            long b = i2;
            b <<= 32;
            b |= (uint)i1;
            return b;
        }
        /// <summary>
        /// Intern FieldName constant into module storage and return FieldName index.
        /// </summary>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="StringCollisionException"></exception>
        public long GetTypeConstant(FieldName name)
        {
            var i1 = GetStringConstant(name.Class);
            var i2 = GetStringConstant(name.Name);
            long b = i2;
            b <<= 32;
            b |= (uint)i1;
            return b;
        }
        
        internal (int, QualityTypeName) GetMethodToken(WaveMethod method) => 
            (this.GetStringConstant(method.Name), method.Owner.FullName);
        /// <summary>
        /// Bake result into il byte code.
        /// </summary>
        public byte[] BakeByteArray()
        {
            classList.OfType<IBaker>().Pipe(x => x.BakeDebugString()).Consume();
            classList.OfType<IBaker>().Pipe(x => x.BakeByteArray()).Consume();
            
            using var mem = new MemoryStream();
            using var binary = new BinaryWriter(mem);

            var idx = GetStringConstant(Name);
            
            binary.Write(idx);
            
            binary.Write(strings.Count);
            foreach (var (key, value) in strings)
            {
                binary.Write(key);
                binary.WriteInsomniaString(value);
            }
            binary.Write(classList.Count);
            foreach (var clazz in classList.OfType<IBaker>())
            {
                var body = clazz.BakeByteArray();
                binary.Write(body.Length);
                binary.Write(body);
            }

            return mem.ToArray();
        }
        /// <summary>
        /// Bake result into debug il preview document.
        /// </summary>
        public string BakeDebugString()
        {
            var str = new StringBuilder();
            str.AppendLine($".module '{Name}'");
            str.AppendLine("{");
            foreach (var (key, value) in strings) 
                str.AppendLine($"\t.string 0x{key:X8}.'{value}'");
            foreach (var clazz in classList.OfType<IBaker>().Select(x => x.BakeDebugString()))
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
    }
}