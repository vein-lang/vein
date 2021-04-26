namespace wave.runtime
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;
    using extensions;

    public class WaveMethod : WaveMethodBase
    {
        public WaveClass ReturnType { get; set; }
        public WaveClass Owner { get; set; }
        public readonly Dictionary<int, WaveArgumentRef> Locals = new();

        protected WaveMethod() : base(null, 0) { }
        
        internal WaveMethod(string name, MethodFlags flags, params WaveArgumentRef[] args)
            : base(name, flags, args) =>
            this.ReturnType = WaveTypeCode.TYPE_VOID.AsClass();

        internal WaveMethod(string name, MethodFlags flags, WaveClass returnType, WaveClass owner,
            params WaveArgumentRef[] args)
            : base(name, flags, args)
        {
            this.Owner = owner;
            this.ReturnType = returnType;
        }
        
        public override string ToString() 
            => $"{Owner.Name}::{RawName}({Arguments.Select(x => $"{x.Name}: {x.Type.Name}").Join(',')})";
    }
    
    
    public abstract class WaveMethodBase : WaveMember
    {
        protected WaveMethodBase(string name, MethodFlags flags, params WaveArgumentRef[] args)
        {
            this.Arguments.AddRange(args);
            this.Name = name;
            this.Flags = flags;
            this.RegenerateName();
        }

        private void RegenerateName()
        {
            if (Regex.IsMatch(this.Name, @"\S+\((.+)?\)"))
                return;
            this.Name = $"{this.Name}({Arguments.Select(x => x.Type.Name).Join(",")})";
        }
        
        
        public MethodFlags Flags { get; set; }
        
        public bool IsStatic => Flags.HasFlag(MethodFlags.Static);
        public bool IsPrivate => Flags.HasFlag(MethodFlags.Private);
        public bool IsExtern => Flags.HasFlag(MethodFlags.Extern);
        public override bool IsSpecial => Flags.HasFlag(MethodFlags.Special);
        
        public sealed override string Name { get; protected set; }
        public string RawName => Name.Split('(').First();
        
        public List<WaveArgumentRef> Arguments { get; } = new();

        public int ArgLength => Arguments.Count;
        
        public override WaveMemberKind Kind => WaveMemberKind.Method;
    }
}