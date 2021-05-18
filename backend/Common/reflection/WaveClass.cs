namespace wave.runtime
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class WaveClass : IEquatable<WaveClass>
    {
        public QualityTypeName FullName { get; set; }
        public string Name => FullName.Name;
        public string Path => FullName.Namespace;
        public ClassFlags Flags { get; set; }
        public WaveClass Parent { get; set; }
        public readonly List<WaveField> Fields = new();
        public List<WaveMethod> Methods { get; set; } = new();
        public WaveTypeCode TypeCode { get; set; } = WaveTypeCode.TYPE_CLASS;
        public bool IsPrimitive => TypeCode != WaveTypeCode.TYPE_CLASS && TypeCode != WaveTypeCode.TYPE_NONE;
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
        
        internal WaveMethod DefineMethod(string name, WaveClass returnType, MethodFlags flags, params WaveArgumentRef[] args)
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
            => $"{FullName}, {Flags} ({Parent?.FullName})";


        public WaveMethod FindMethod(string name, IEnumerable<WaveClass> args_types)
            => this.Methods.FirstOrDefault(x =>
            {
                var nameHas = x.RawName.Equals(name);
                var argsHas = x.Arguments.Select(z => z.Type).SequenceEqual(args_types);

                return nameHas
                       && argsHas
                       ;
            });

        public WaveField FindField(string name) 
            => this.Fields.FirstOrDefault(x => x.Name.Equals(name));

        public WaveMethod FindMethod(string name, Func<WaveMethod, bool> eq = null)
        {
            eq ??= s => s.RawName.Equals(name);

            foreach (var member in Methods)
            {
                if (eq(member))
                    return member;
            }

            return null;
        }

        #region Equality members

        public bool Equals(WaveClass other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(FullName, other.FullName) && 
                   Equals(Parent, other.Parent);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((WaveClass) obj);
        }

        public override int GetHashCode() 
            => HashCode.Combine(FullName, Parent);

        public static bool operator ==(WaveClass left, WaveClass right) => Equals(left, right);

        public static bool operator !=(WaveClass left, WaveClass right) => !Equals(left, right);

        #endregion
    }
}