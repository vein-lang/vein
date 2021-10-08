namespace vein.runtime
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Text.RegularExpressions;
    using extensions;
    using reflection;

    public class VeinMethod : VeinMethodBase, IAspectable
    {
        public VeinClass ReturnType { get; set; }
        public VeinClass Owner { get; set; }
        public readonly Dictionary<int, VeinArgumentRef> Locals = new();
        public List<Aspect> Aspects { get; } = new();

        protected VeinMethod() : base(null, 0) { }

        internal VeinMethod(string name, MethodFlags flags, params VeinArgumentRef[] args)
            : base(name, flags, args) =>
            this.ReturnType = VeinTypeCode.TYPE_VOID.AsClass();

        internal VeinMethod(string name, MethodFlags flags, VeinClass returnType, VeinClass owner,
            params VeinArgumentRef[] args)
            : base(name, flags, args)
        {
            this.Owner = owner;
            this.ReturnType = returnType;
        }

        public override string ToString()
            => $"{Owner.Name}::{RawName}({Arguments.Select(x => $"{x.Name}: {x.Type.Name}").Join(',')})";
    }


    public abstract class VeinMethodBase : VeinMember
    {
        [MethodImpl(MethodImplOptions.NoOptimization)]
        protected VeinMethodBase(string name, MethodFlags flags, params VeinArgumentRef[] args)
        {
            this.Arguments.AddRange(args);
            this.Name = this.RegenerateName(name);
            this.Flags = flags;
        }

        private string RegenerateName(string n) =>
            Regex.IsMatch(n, @"\S+\((.+)?\)", RegexOptions.Compiled)
                ? n : GetFullName(n, Arguments);

        public static string GetFullName(string name, List<VeinArgumentRef> args)
            => $"{name}({args.Select(x => x.Type?.Name).Join(",")})";


        public MethodFlags Flags { get; set; }

        public bool IsStatic => Flags.HasFlag(MethodFlags.Static);
        public bool IsPrivate => Flags.HasFlag(MethodFlags.Private);
        public bool IsExtern => Flags.HasFlag(MethodFlags.Extern);
        public bool IsAbstract => Flags.HasFlag(MethodFlags.Abstract);
        public bool IsVirtual => Flags.HasFlag(MethodFlags.Virtual);
        public bool IsOverride => !Flags.HasFlag(MethodFlags.Abstract) && Flags.HasFlag(MethodFlags.Override);
        public override bool IsSpecial => Flags.HasFlag(MethodFlags.Special);

        public sealed override string Name { get; protected set; }
        public string RawName => Name.Split('(').First();

        public List<VeinArgumentRef> Arguments { get; } = new();

        public int ArgLength => Arguments.Count;

        public override ManaMemberKind Kind => ManaMemberKind.Method;
    }
}
