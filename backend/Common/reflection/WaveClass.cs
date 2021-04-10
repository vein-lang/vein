namespace insomnia.emit
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

        public WaveMethod GetDefaultDtor() => GetOrCreateTor("dtor()");
        public WaveMethod GetDefaultCtor() => GetOrCreateTor("ctor()");
        
        public WaveMethod GetStaticCtor() => GetOrCreateTor("type_ctor()", true);


        private WaveMethod GetOrCreateTor(string name, bool isStatic = false)
        {
            var ctor = FindMethod(name);
            if (ctor is not null)
                return ctor;

            var flags = MethodFlags.Public;

            if (isStatic)
                flags |= MethodFlags.Static;

            if (this is ClassBuilder builder)
            {
                ctor = builder.DefineMethod(name, flags, WaveTypeCode.TYPE_VOID.AsType());
                builder.moduleBuilder.InternString(ctor.Name);
            }
            else
                ctor = new WaveMethod(name, flags, WaveTypeCode.TYPE_VOID.AsType(), this);
            Methods.Add(ctor);
            
            return ctor;
        }

        internal WaveMethod FindMethod(string name) 
            => Methods.FirstOrDefault(method => method.Name == name);
        
        public override string ToString() 
            => $"{FullName}, {Flags} ({Parent.FullName})";
    }
}