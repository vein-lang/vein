namespace wave.runtime
{
    using System.Collections.Generic;
    using System.Linq;

    public class WaveClass
    {
        public QualityTypeName FullName { get; set; }
        public string Name => FullName.Name;
        public string Path => FullName.Namespace;
        public ClassFlags Flags { get; set; }
        public WaveClass Parent { get; set; }
        public readonly List<WaveField> Fields = new();
        public List<WaveMethod> Methods { get; set; } = new();
        public WaveTypeCode TypeCode { get; set; } = WaveTypeCode.TYPE_CLASS;

        public WaveModule Owner { get; set; }
        
        internal WaveClass(QualityTypeName name, WaveClass parent, WaveModule module)
        {
            this.FullName = name;
            this.Parent = parent;
            this.Owner = module;
        }
        internal WaveClass(WaveType type, WaveClass parent)
        {
            this.FullName = type.FullName;
            this.Parent = parent;
            this.TypeCode = type.TypeCode;
        }
        
        protected WaveClass() {  }
        
        internal WaveMethod DefineMethod(string name, WaveType returnType, MethodFlags flags, params WaveArgumentRef[] args)
        {
            var method = new WaveMethod(name, flags, returnType, this, args);
            method.Arguments.AddRange(args);

            if (Methods.Any(x => x.Name.Equals(method.Name)))
                return Methods.First(x => x.Name.Equals(method.Name));

            Methods.Add(method);
            return method;
        }

        public bool IsSpecial => Flags.HasFlag(ClassFlags.Special);
        public bool IsPublic => Flags.HasFlag(ClassFlags.Public);
        public bool IsPrivate => Flags.HasFlag(ClassFlags.Private);
        public bool IsAbstract => Flags.HasFlag(ClassFlags.Abstract);
        public bool IsStatic => Flags.HasFlag(ClassFlags.Static);
        public bool IsInternal => Flags.HasFlag(ClassFlags.Internal);

        public virtual WaveMethod GetDefaultDtor() => GetOrCreateTor("dtor()");
        public virtual WaveMethod GetDefaultCtor() => GetOrCreateTor("ctor()");
        
        public virtual WaveMethod GetStaticCtor() => GetOrCreateTor("type_ctor()", true);


        protected virtual WaveMethod GetOrCreateTor(string name, bool isStatic = false) 
            => Methods.FirstOrDefault(x => x.IsStatic == isStatic && x.Name.Equals(name));

        public override string ToString() 
            => $"{FullName}, {Flags} ({Parent.FullName})";
    }
}