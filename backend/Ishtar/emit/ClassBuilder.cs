namespace wave.ishtar.emit
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using extensions;
    using wave.extensions;
    using wave.runtime;

    public class ClassBuilder : WaveClass, IBaker
    {
        internal WaveModuleBuilder moduleBuilder;

        public List<string> Includes { get; set; } = new ();

        internal ClassBuilder WithIncludes(List<string> includes)
        {
            Includes.AddRange(includes);
            return this;
        }

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
            this.Parent = parent.AsClass();
        }
        internal ClassBuilder(WaveModuleBuilder module, QualityTypeName name, WaveClass parent)
        {
            this.FullName = name;
            moduleBuilder = module;
            this.Parent = parent;
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
        public MethodBuilder DefineMethod(string name, WaveClass returnType, params WaveArgumentRef[] args)
        {
            moduleBuilder.InternString(name);
            var method = new MethodBuilder(this, name, returnType, args);

            if (Methods.Any(x => x.Name == method.Name))
                throw new Exception($"Method '{method.Name}' in class '{Name}' already defined.");
            Methods.Add(method);
            return method;
        }
        /// <summary>
        /// Define method in current class.
        /// </summary>
        /// <remarks>
        /// Method name will be interned.
        /// </remarks>
        public MethodBuilder DefineMethod(string name, MethodFlags flags, WaveClass returnType, params WaveArgumentRef[] args)
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
        public WaveField DefineField(string name, FieldFlags flags, WaveClass fieldType)
        {
            var field = new WaveField(this, new FieldName(name, this.Name), flags, fieldType);
            moduleBuilder.InternFieldName(field.FullName);
            if (Fields.Any(x => x.Name == name))
                throw new Exception($"Field '{name}' in class '{Name}' already defined.");
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
            binary.WriteTypeName(Parent?.FullName ?? this.FullName, moduleBuilder);
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

        #region Overrides of WaveClass

        protected override WaveMethod GetOrCreateTor(string name, bool isStatic = false)
        {
            var ctor =  base.GetOrCreateTor(name, isStatic);
            if (ctor is not null)
                return ctor;

            var flags = MethodFlags.Public;

            if (isStatic)
                flags |= MethodFlags.Static;

            ctor = DefineMethod(name, flags, WaveTypeCode.TYPE_VOID.AsClass());
            moduleBuilder.InternString(ctor.Name);
            
            return ctor;
        }

        #endregion

        public ulong? FindMemberField(FieldName field) 
            => throw new NotImplementedException();
    }
}