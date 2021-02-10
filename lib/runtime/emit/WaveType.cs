namespace wave.emit
{
    using System;
    using System.Linq;


    internal sealed class WaveTypeImpl : WaveType
    {
        public override TypeName FullName { get; }
        public override WaveMemberKind Kind => WaveMemberKind.Type;
        public WaveTypeCode TypeCode { get; }
        
        
        private readonly ClassFlags _flags;
        
        public WaveTypeImpl(TypeName typeName, WaveTypeCode code)
        {
            this.FullName = typeName;
            this.TypeCode = code;
        }
        public WaveTypeImpl(TypeName typeName, WaveTypeCode code, ClassFlags flags) 
            : this(typeName, code) 
            => this._flags = flags;

        public override bool IsStatic => _flags.HasFlag(ClassFlags.Static);
        public override bool IsPublic => _flags.HasFlag(ClassFlags.Public);
        public override bool IsPrivate => _flags.HasFlag(ClassFlags.Private);
        public override bool IsPrimitive => TypeCode != WaveTypeCode.TYPE_CLASS && TypeCode != WaveTypeCode.TYPE_NONE;
        public override bool IsClass => !IsPrimitive;

        
        public override string ToString() => $"[{FullName}]";
    }
    
    public abstract class WaveType : WaveMember
    {
        public string Namespace => FullName.Namespace;
        public override string Name => FullName.Name;
        

        public abstract TypeName FullName { get; }

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
        
        
        public static WaveType ByName(TypeName name) => 
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