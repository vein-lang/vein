namespace wave.emit
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
    
    public abstract class WaveType : WaveMember
    {
        public string Namespace => FullName.Namespace;
        public List<WaveMember> Members { get; } = new();
        protected internal ClassFlags? classFlags { get; set; }
        public WaveTypeCode TypeCode { get; protected set; }
        public WaveType Parent { get; protected set; }

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
        
        
        public override string ToString() => $"[{FullName}]";
    }

    public abstract class WaveMember
    {
        public abstract string Name { get; protected set; }
        public abstract WaveMemberKind Kind { get; }
    }
    
    [Flags]
    public enum WaveMemberKind
    {
        Ctor = 1 << 1,
        Dtor = 1 << 2,
        Field = 1 << 3,
        Method = 1 << 4,
        Type  = 1 << 5
    }
    
}