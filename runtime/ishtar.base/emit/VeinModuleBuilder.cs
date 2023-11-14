namespace ishtar.emit
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.IO;
    using System.Linq;
    using System.Text;
    using extensions;
    using global::ishtar;
    using MoreLinq;
    using vein;
    using vein.exceptions;
    using vein.extensions;
    using vein.runtime;

    public class VeinModuleBuilder : VeinModule, IBaker
    {
        public VeinModuleBuilder(string name, VeinCore types) : base(name, types) { }
        public VeinModuleBuilder(string name, Version ver, VeinCore types) : base(name, ver, types) { }

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
            if (class_table.Any(x => x.FullName.Equals(name)))
                throw new DuplicateNameException($"Class '{name}' already defined.");
            InternString(name.Name);
            InternString(name.Namespace);
            InternString(name.AssemblyName);
            var c = new ClassBuilder(this, name);
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
            class_table.OfType<IBaker>().Pipe(x => x.BakeDebugString()).Consume();
            class_table.OfType<IBaker>().Pipe(x => x.BakeByteArray()).Consume();

            using var mem = new MemoryStream();
            using var binary = new BinaryWriter(mem);

            var idx = InternString(Name);
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
                binary.WriteIshtarString(value.AssemblyName);
                binary.WriteIshtarString(value.Namespace);
                binary.WriteIshtarString(value.Name);
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
                binary.WriteIshtarString(dep.Name);
                binary.WriteIshtarString(dep.Version.ToString());
            }

            binary.Write(class_table.Count);
            foreach (var clazz in class_table.OfType<IBaker>())
            {
                var body = clazz.BakeByteArray();
                binary.Write(body.Length);
                binary.Write(body);
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
            str.AppendLine($".module '{Name}'::'{Version}'");
            str.AppendLine("{");
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
    }
}
