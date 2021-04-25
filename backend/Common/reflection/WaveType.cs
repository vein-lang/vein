namespace wave.runtime
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    
    internal sealed class WaveTypeImpl : WaveType
    {
        public override QualityTypeName FullName { get; }
        
        public WaveTypeImpl(QualityTypeName typeName, WaveTypeCode code)
        {
            this.FullName = typeName;
            this.TypeCode = code;
        }
        public WaveTypeImpl(QualityTypeName typeName, WaveTypeCode code, ClassFlags flags) 
            : this(typeName, code) 
            => this.classFlags = flags;
        public WaveTypeImpl(QualityTypeName typeName, WaveTypeCode code, ClassFlags flags, WaveType parent) 
            : this(typeName, code, flags) =>
            this.Parent = parent;

        public override bool IsStatic => classFlags?.HasFlag(ClassFlags.Static) ?? false;
        public override bool IsPublic => classFlags?.HasFlag(ClassFlags.Public) ?? false;
        public override bool IsPrivate => classFlags?.HasFlag(ClassFlags.Private) ?? false;
        public override bool IsPrimitive => TypeCode != WaveTypeCode.TYPE_CLASS && TypeCode != WaveTypeCode.TYPE_NONE;
        public override bool IsClass => !IsPrimitive;
    }
    
    public abstract class WaveType : WaveMember, IEquatable<WaveType>
    {
        // autogen wtf 
        #region Equality members

        public bool Equals(WaveType other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(Members, other.Members) && 
                   classFlags == other.classFlags && 
                   TypeCode == other.TypeCode && 
                   Equals(Parent, other.Parent) && 
                   Equals(FullName, other.FullName) && 
                   IsArray == other.IsArray && 
                   IsSealed == other.IsSealed && 
                   IsClass == other.IsClass && 
                   IsPublic == other.IsPublic && 
                   IsStatic == other.IsStatic && 
                   IsPrivate == other.IsPrivate && 
                   IsPrimitive == other.IsPrimitive;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((WaveType) obj);
        }

        public override int GetHashCode()
        {
            var hashCode = new HashCode();
            hashCode.Add(Members);
            hashCode.Add(classFlags);
            hashCode.Add((int) TypeCode);
            hashCode.Add(Parent);
            hashCode.Add(FullName);
            hashCode.Add(IsArray);
            hashCode.Add(IsSealed);
            hashCode.Add(IsClass);
            hashCode.Add(IsPublic);
            hashCode.Add(IsStatic);
            hashCode.Add(IsPrivate);
            hashCode.Add(IsPrimitive);
            return hashCode.ToHashCode();
        }

        #endregion

        public string Namespace => FullName.Namespace;
        public List<WaveMember> Members { get; } = new();
        protected internal ClassFlags? classFlags { get; set; }
        public WaveTypeCode TypeCode { get; protected set; }
        public WaveType Parent { get; protected internal set; }
        public WaveModule Owner { get; set; }

        public override string Name
        {
            get => FullName.Name;
            protected set => throw new NotImplementedException();
        }


        public abstract QualityTypeName FullName { get; }

        public virtual bool IsArray { get; protected set; } = false;
        public virtual bool IsSealed { get; protected set; } = false;
        public virtual bool IsClass { get; protected set; } = false;
        public virtual bool IsPublic { get; protected set; } = false;
        public virtual bool IsStatic { get; protected set; } = false;
        public virtual bool IsPrivate { get; protected set; } = false;
        public virtual bool IsPrimitive { get; protected set; } = false;
        
        public override WaveMemberKind Kind => WaveMemberKind.Type;


        public static implicit operator WaveType(string typeName) =>
            new WaveTypeImpl(typeName, WaveTypeCode.TYPE_CLASS);
        
        
        public static WaveType ByName(QualityTypeName name) => 
            WaveCore.Types.All.FirstOrDefault(x => x.FullName == name);
        
        
        public override string ToString() => $"[{FullName.NameWithNS}]";

        public WaveMethod FindMethod(string name, IEnumerable<WaveType> args_types)
            => this.Members.OfType<WaveMethod>().FirstOrDefault(x => 
                x.RawName.Equals(name) && 
                x.Arguments.Select(z => z.Type).SequenceEqual(args_types)
                );

        public WaveField FindField(string name) 
            => this.Members.OfType<WaveField>().FirstOrDefault(x => x.Name.Equals(name));

        public WaveMethod FindMethod(string name, Func<WaveMethod, bool> eq = null)
        {
            eq ??= s => s.RawName.Equals(name);

            foreach (var member in Members)
            {
                if (member is not WaveMethod method)
                    continue;
                if (eq(method))
                    return method;
            }

            return null;
        }

        public static bool operator ==(WaveType t1, WaveTypeCode t2) => t1?.TypeCode == t2;
        public static bool operator !=(WaveType t1, WaveTypeCode t2) => !(t1 == t2);

        public static bool operator ==(WaveType t1, WaveType t2)
        {
            if (t1 is null || t2 is null)
                return false;
            return t1.FullName.fullName.Equals(t2.FullName.fullName);
        }

        public static bool operator !=(WaveType t1, WaveType t2) => !(t1 == t2);
    }

    public abstract class WaveMember
    {
        public abstract string Name { get; protected set; }
        public abstract WaveMemberKind Kind { get; }
        public virtual bool IsSpecial { get; }
    }
    
    [Flags]
    public enum WaveMemberKind
    {
        Ctor    = 1 << 1,
        Dtor    = 1 << 2,
        Field   = 1 << 3,
        Method  = 1 << 4,
        Type    = 1 << 5
    }
}