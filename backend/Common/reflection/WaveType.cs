namespace mana.runtime
{
    using System;
    using System.Collections.Generic;
    using System.Linq;


    internal sealed class ManaTypeImpl : ManaType
    {
        public override QualityTypeName FullName { get; }

        public ManaTypeImpl(QualityTypeName typeName, ManaTypeCode code)
        {
            this.FullName = typeName;
            this.TypeCode = code;
        }
        public ManaTypeImpl(QualityTypeName typeName, ManaTypeCode code, ClassFlags flags)
            : this(typeName, code)
            => this.classFlags = flags;
        public ManaTypeImpl(QualityTypeName typeName, ManaTypeCode code, ClassFlags flags, ManaType parent)
            : this(typeName, code, flags) =>
            this.Parent = parent;

        public override bool IsStatic => classFlags?.HasFlag(ClassFlags.Static) ?? false;
        public override bool IsPublic => classFlags?.HasFlag(ClassFlags.Public) ?? false;
        public override bool IsPrivate => classFlags?.HasFlag(ClassFlags.Private) ?? false;
        public override bool IsPrimitive => TypeCode != ManaTypeCode.TYPE_CLASS && TypeCode != ManaTypeCode.TYPE_NONE;
        public override bool IsClass => !IsPrimitive;
    }

    public abstract class ManaType : ManaMember, IEquatable<ManaType>
    {
        // autogen wtf 
        #region Equality members

        public bool Equals(ManaType other)
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
            return Equals((ManaType)obj);
        }

        public override int GetHashCode()
        {
            var hashCode = new HashCode();
            hashCode.Add(Members);
            hashCode.Add(classFlags);
            hashCode.Add((int)TypeCode);
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
        public List<ManaMember> Members { get; } = new();
        protected internal ClassFlags? classFlags { get; set; }
        public ManaTypeCode TypeCode { get; protected set; }
        public ManaType Parent { get; protected internal set; }
        public ManaModule Owner { get; set; }

        public override string Name
        {
            get => FullName.Name;
            protected set => throw new NotImplementedException();
        }
        public ManaMethod FindMethod(string name, IEnumerable<ManaClass> args_types)
            => this.Members.OfType<ManaMethod>().FirstOrDefault(x =>
                x.RawName.Equals(name) &&
                x.Arguments.Select(z => z.Type).SequenceEqual(args_types)
            );

        public ManaField FindField(string name)
            => this.Members.OfType<ManaField>().FirstOrDefault(x => x.Name.Equals(name));

        public ManaMethod FindMethod(string name, Func<ManaMethod, bool> eq = null)
        {
            eq ??= s => s.RawName.Equals(name);

            foreach (var member in Members)
            {
                if (member is not ManaMethod method)
                    continue;
                if (eq(method))
                    return method;
            }

            return null;
        }

        public abstract QualityTypeName FullName { get; }

        public virtual bool IsArray { get; protected set; } = false;
        public virtual bool IsSealed { get; protected set; } = false;
        public virtual bool IsClass { get; protected set; } = false;
        public virtual bool IsPublic { get; protected set; } = false;
        public virtual bool IsStatic { get; protected set; } = false;
        public virtual bool IsPrivate { get; protected set; } = false;
        public virtual bool IsPrimitive { get; protected set; } = false;

        public override ManaMemberKind Kind => ManaMemberKind.Type;


        public static implicit operator ManaType(string typeName) =>
            new ManaTypeImpl(typeName, ManaTypeCode.TYPE_CLASS);


        public static ManaType ByName(QualityTypeName name) =>
            ManaCore.Types.All.FirstOrDefault(x => x.FullName == name);


        public override string ToString() => $"[{FullName.NameWithNS}]";

        public static bool operator ==(ManaType t1, ManaTypeCode t2) => t1?.TypeCode == t2;
        public static bool operator !=(ManaType t1, ManaTypeCode t2) => !(t1 == t2);

        public static bool operator ==(ManaType t1, ManaType t2)
        {
            if (t1 is null || t2 is null)
                return false;
            return t1.FullName.fullName.Equals(t2.FullName.fullName);
        }

        public static bool operator !=(ManaType t1, ManaType t2) => !(t1 == t2);
    }

    public abstract class ManaMember
    {
        public abstract string Name { get; protected set; }
        public abstract ManaMemberKind Kind { get; }
        public virtual bool IsSpecial { get; }
    }

    [Flags]
    public enum ManaMemberKind
    {
        Ctor = 1 << 1,
        Dtor = 1 << 2,
        Field = 1 << 3,
        Method = 1 << 4,
        Type = 1 << 5
    }
}