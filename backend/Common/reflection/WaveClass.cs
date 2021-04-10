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
        public readonly List<WaveMethod> Methods = new();
        public WaveTypeCode TypeCode { get; set; } = WaveTypeCode.TYPE_CLASS;
        
        internal WaveClass(QualityTypeName name, WaveClass parent)
        {
            this.FullName = name;
            this.Parent = parent;
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
        {
            return FindMethod(name);
        }

        internal WaveMethod FindMethod(string name) 
            => Methods.FirstOrDefault(method => method.Name == name);
        
        public override string ToString() 
            => $"{FullName}, {Flags} ({Parent.FullName})";
    }
}