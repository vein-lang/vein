namespace wave.emit
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using extensions;

    public class ClassBuilder
    {
        private readonly string _name;
        private readonly string _ns;
        private ClassFlags _flags;
        internal ModuleBuilder moduleBuilder;
        internal List<MethodBuilder> methods = new();
        public ClassBuilder(ModuleBuilder module, string name, string @namespace)
        {
            _name = name;
            _ns = @namespace;
            moduleBuilder = module;
        }

        public void SetFlags(ClassFlags flags)
            => _flags = flags; 
        
        public MethodBuilder DefineMethod(string name)
        {
            var method = new MethodBuilder(this, name);
            methods.Add(method);
            return method;
        }

        internal string BakeDebugString()
        {
            var str = new StringBuilder();
            str.AppendLine($".namespace {_ns}");
            str.AppendLine($".class {_name} {_flags.EnumerateFlags().Join(' ').ToLowerInvariant()}");
            str.AppendLine("{");
            foreach (var method in methods.Select(method => method.BakeDebugString()))
                str.AppendLine($"{method.Split("\n").Select(x => $"\t{x}").Join("\n")}");
            str.AppendLine("}");
            return str.ToString();
        }
    }
}