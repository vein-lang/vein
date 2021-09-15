namespace mana.runtime
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;
    using extensions;
    using reflection;

    public class ManaMethod : ManaMethodBase, IAspectable
    {
        public ManaClass ReturnType { get; set; }
        public ManaClass Owner { get; set; }
        public readonly Dictionary<int, ManaArgumentRef> Locals = new();
        public List<Aspect> Aspects { get; } = new();

        protected ManaMethod() : base(null, 0) { }

        internal ManaMethod(string name, MethodFlags flags, params ManaArgumentRef[] args)
            : base(name, flags, args) =>
            this.ReturnType = ManaTypeCode.TYPE_VOID.AsClass();

        internal ManaMethod(string name, MethodFlags flags, ManaClass returnType, ManaClass owner,
            params ManaArgumentRef[] args)
            : base(name, flags, args)
        {
            this.Owner = owner;
            this.ReturnType = returnType;
        }

        public override string ToString()
            => $"{Owner.Name}::{RawName}({Arguments.Select(x => $"{x.Name}: {x.Type.Name}").Join(',')})";
    }


    public abstract class ManaMethodBase : ManaMember
    {
        protected ManaMethodBase(string name, MethodFlags flags, params ManaArgumentRef[] args)
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
            this.Name = GetFullName(Name, Arguments);
        }

        public static string GetFullName(string name, List<ManaArgumentRef> args)
            => $"{name}({args.Select(x => x.Type?.Name).Join(",")})";


        public MethodFlags Flags { get; set; }

        public bool IsStatic => Flags.HasFlag(MethodFlags.Static);
        public bool IsPrivate => Flags.HasFlag(MethodFlags.Private);
        public bool IsExtern => Flags.HasFlag(MethodFlags.Extern);
        public bool IsAbstract => Flags.HasFlag(MethodFlags.Abstract);
        public override bool IsSpecial => Flags.HasFlag(MethodFlags.Special);

        public sealed override string Name { get; protected set; }
        public string RawName => Name.Split('(').First();

        public List<ManaArgumentRef> Arguments { get; } = new();

        public int ArgLength => Arguments.Count;

        public override ManaMemberKind Kind => ManaMemberKind.Method;
    }
}
