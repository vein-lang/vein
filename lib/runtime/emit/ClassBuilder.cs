namespace wave.emit
{
    using System.IO;
    using System.Linq;
    using System.Text;

    public class ClassBuilder : WaveClass, IBaker
    {
        internal ModuleBuilder moduleBuilder;
        public ClassBuilder(ModuleBuilder module, TypeName name, WaveTypeCode parent = WaveTypeCode.TYPE_OBJECT)
        {
            this.FullName = name;
            moduleBuilder = module;
            this.Parent = parent.AsType().AsClass();
        }
        public ClassBuilder(ModuleBuilder module, TypeName name, WaveType parent)
        {
            this.FullName = name;
            moduleBuilder = module;
            this.Parent = parent.AsClass();
        }

        public TypeName GetName() => this.FullName;
        
        public MethodBuilder DefineMethod(string name, WaveType returnType, params WaveArgumentRef[] args)
        {
            var method = new MethodBuilder(this, name, returnType, args);
            Methods.Add(method);
            return method;
        }
        public MethodBuilder DefineMethod(string name, MethodFlags flags, WaveType returnType, params WaveArgumentRef[] args)
        {
            var method = this.DefineMethod(name, returnType, args);
            method.Flags = flags;
            return method;
        }

        public byte[] BakeByteArray()
        {
            if (Methods.Count == 0)
                return null;
            using var mem = new MemoryStream();
            using var binary = new BinaryWriter(mem);
            var idx = moduleBuilder.GetStringConstant(FullName.Name);
            var ns_idx = moduleBuilder.GetStringConstant(FullName.Namespace);
            binary.Write(idx);
            binary.Write(ns_idx);
            binary.Write((byte)Flags);
            binary.Write(moduleBuilder.GetTypeConstant(Parent.FullName));
            binary.Write(Methods.Count);
            foreach (var method in Methods.OfType<IBaker>())
            {
                var body = method.BakeByteArray();
                binary.Write(body.Length);
                binary.Write(body);
            }
            binary.Write(Fields.Count);
            foreach (var field in Fields)
            {
                binary.Write(moduleBuilder.GetTypeConstant(field.FullName));
                binary.Write(moduleBuilder.GetTypeConstant(field.FieldType.FullName));
                binary.Write((byte)field.Flags);
                binary.WriteLiteralValue(field);
            }
            return mem.ToArray();
        }
        
        public string BakeDebugString()
        {
            var str = new StringBuilder();
            str.AppendLine($".namespace '{FullName.Namespace}'");
            str.AppendLine($".class '{FullName.Name}' {Flags.EnumerateFlags().Join(' ').ToLowerInvariant()}");
            str.AppendLine("{");
            foreach (var field in Fields)
            {
                var flags = field.Flags.EnumerateFlags().Join(' ').ToLowerInvariant();
                str.AppendLine($"\t.field '{field.Name}' as '{field.FieldType.Name}' {flags}");
                if (field.IsLiteral)
                    str.AppendLine($"\t\t= [{field.BakeLiteralValue().Select(x => $"{x:2}").Join(',')}];");
            }
            str.AppendLine("");
            foreach (var method in Methods.OfType<IBaker>().Select(method => method.BakeDebugString()))
                str.AppendLine($"{method.Split("\n").Select(x => $"\t{x}").Join("\n")}");
            str.AppendLine("}");
            return str.ToString();
        }

        public ulong? FindMemberField(FieldName field)
        {
            return null;
        }
    }
}