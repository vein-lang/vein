namespace insomnia.emit
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Text;
    using extensions;

    public class ClassBuilder : WaveClass, IBaker
    {
        internal WaveModuleBuilder moduleBuilder;

        internal ClassBuilder(WaveModuleBuilder module, WaveClass clazz)
        {
            this.moduleBuilder = module;
            this.FullName = clazz.FullName;
            this.Parent = clazz.Parent;
            this.TypeCode = clazz.TypeCode;
        }
        internal ClassBuilder(WaveModuleBuilder module, QualityTypeName name, WaveTypeCode parent = WaveTypeCode.TYPE_OBJECT)
        {
            this.FullName = name;
            moduleBuilder = module;
            this.Parent = parent.AsType().AsClass();
        }
        internal ClassBuilder(WaveModuleBuilder module, QualityTypeName name, WaveType parent)
        {
            this.FullName = name;
            moduleBuilder = module;
            this.Parent = parent.AsClass();
        }
        /// <summary>
        /// Get class <see cref="QualityTypeName"/>.
        /// </summary>
        public QualityTypeName GetName() => this.FullName;
        
        /// <summary>
        /// Define method in current class.
        /// </summary>
        /// <remarks>
        /// Method name will be interned.
        /// </remarks>
        public MethodBuilder DefineMethod(string name, WaveType returnType, params WaveArgumentRef[] args)
        {
            moduleBuilder.InternString(name);
            var method = new MethodBuilder(this, name, returnType, args);
            Methods.Add(method);
            return method;
        }
        /// <summary>
        /// Define method in current class.
        /// </summary>
        /// <remarks>
        /// Method name will be interned.
        /// </remarks>
        public MethodBuilder DefineMethod(string name, MethodFlags flags, WaveType returnType, params WaveArgumentRef[] args)
        {
            var method = this.DefineMethod(name, returnType, args);
            method.Owner = this;
            method.Flags = flags;
            return method;
        }
        /// <summary>
        /// Define field in current class.
        /// </summary>
        /// <remarks>
        /// Field name will be interned.
        /// </remarks>
        public WaveField DefineField(string name, FieldFlags flags, WaveType fieldType)
        {
            var field = new WaveField(this, new FieldName(name, this.Name), flags, fieldType);
            moduleBuilder.InternFieldName(field.FullName);
            Fields.Add(field);
            return field;
        }
        
        byte[] IBaker.BakeByteArray()
        {
            if (Methods.Count == 0 && Fields.Count == 0)
                return Array.Empty<byte>();
            using var mem = new MemoryStream();
            using var binary = new BinaryWriter(mem);
            
            binary.WriteTypeName(this.FullName, moduleBuilder);
            binary.Write((short)Flags);
            binary.WriteTypeName(Parent.FullName, moduleBuilder);
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
                binary.Write(moduleBuilder.InternFieldName(field.FullName));
                binary.WriteTypeName(field.FieldType.FullName, moduleBuilder);
                binary.Write((short)field.Flags);
            }
            return mem.ToArray();
        }
        
        string IBaker.BakeDebugString()
        {
            var str = new StringBuilder();
            str.AppendLine($".namespace '{FullName.Namespace}'");
            str.AppendLine($".class '{FullName.Name}' {Flags.EnumerateFlags().Except(new [] {ClassFlags.None}).Join(' ').ToLowerInvariant()}");
            str.AppendLine("{");
            foreach (var field in Fields)
            {
                var flags = field.Flags.EnumerateFlags().Except(new [] {FieldFlags.None}).Join(' ').ToLowerInvariant();
                str.AppendLine($"\t.field '{field.Name}' as '{field.FieldType.Name}' {flags}");
            }
            str.AppendLine("");
            foreach (var method in Methods.OfType<IBaker>().Select(method => method.BakeDebugString()))
                str.AppendLine($"{method.Split("\n").Select(x => $"\t{x}").Join("\n")}");
            str.AppendLine("}");
            return str.ToString();
        }

        public ulong? FindMemberField(FieldName field) 
            => throw new NotImplementedException();
    }
}