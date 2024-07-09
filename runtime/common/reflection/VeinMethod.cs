namespace vein.runtime
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;
    using extensions;
    using reflection;

    public record VeinComplexType
    {
        private readonly VeinTypeArg _typeArg;
        private readonly VeinClass _class;

        public VeinComplexType(VeinClass @class) => _class = @class;

        public VeinComplexType(VeinTypeArg typeArg) => _typeArg = typeArg;

        public bool IsGeneric => _typeArg is not null;


        public VeinTypeArg TypeArg => _typeArg;
        public VeinClass Class => _class;


        public static implicit operator VeinComplexType(VeinClass cpx) => new(cpx);
        public static implicit operator VeinComplexType(VeinTypeArg cpx) => new(cpx);


        public static implicit operator VeinClass(VeinComplexType cpx)
        {
            if (cpx.IsGeneric)
                throw new NotSupportedException($"Trying summon non generic vein type, but complex type is generic");
            return cpx._class;
        }

        public static implicit operator VeinTypeArg(VeinComplexType cpx)
        {
            if (cpx.IsGeneric)
                throw new NotSupportedException($"Trying summon generic type, but complex type is non generic vein type");
            return cpx._typeArg;
        }

        public string ToTemplateString() => IsGeneric ?
            _typeArg.ToTemplateString() :
            _class.FullName.ToString();
    }

    public record VeinMethodSignature(
        VeinComplexType ReturnType,
        IReadOnlyList<VeinArgumentRef> Arguments)
    {
        public int ArgLength => Arguments.Count;

        public override string ToString() => $"({Arguments.Where(NotThis).Select(x => $"{x.ToTemplateString()}").Join(',')}) -> {ReturnType.ToTemplateString()}";
        public string ToFullString() => $"({Arguments.Select(x => $"{x.ToTemplateString()}").Join(',')}) -> {ReturnType.ToTemplateString()}";


        public static bool NotThis(VeinArgumentRef @ref) => !@ref.Name.Equals(VeinArgumentRef.THIS_ARGUMENT);

        public bool IsGeneric => Arguments.Any(x => x.IsGeneric);

        public bool HasCompatibility(VeinMethodSignature otherSig, bool ignoreThis)
        {
            var a1Args = ignoreThis ? Arguments.Where(NotThis).ToArray() : Arguments.ToArray();
            var a2Args = ignoreThis ? otherSig.Arguments.Where(NotThis).ToArray() : otherSig.Arguments.ToArray();

            if (a1Args.Length != a2Args.Length)
                return false;

            var argumentsCompatibility = true;
            for (int i = 0; i < a1Args.Length; i++)
            {
                var a1 = a1Args[i];
                var a2 = a2Args[i];
                
                if (a1.IsGeneric != a2.IsGeneric)
                {
                    argumentsCompatibility = false;
                    break;
                }
                if (a1.IsGeneric)
                    continue;
                if (a1.Type.FullName != a2.Type.FullName)
                    argumentsCompatibility = false;
            }

            return argumentsCompatibility;
        }


        public string ToTemplateString(bool hasThis = false)
        {
            if (hasThis)
                return ToFullString();
            return ToString();
        }
    }

    public class VeinMethod : VeinMember, IAspectable
    {
        public VeinMethodSignature Signature { get; private set; }
        public VeinComplexType ReturnType => Signature.ReturnType;
        public VeinClass Owner { get; protected internal set; }
        public Dictionary<int, VeinArgumentRef> Locals { get; } = new();
        public List<Aspect> Aspects { get; } = new();

        public const string METHOD_NAME_CONSTRUCTOR = "ctor";
        public const string METHOD_NAME_DECONSTRUCTOR = "dtor";


        public void Temp_ReplaceReturnType(VeinClass clazz) => Signature = Signature with { ReturnType = clazz };


        internal VeinMethod(string name, MethodFlags flags, VeinComplexType returnType, VeinClass owner, params VeinArgumentRef[] args)
        {
            Owner = owner;
            Flags = flags;
            Signature = new VeinMethodSignature(returnType, args);
            Name = RegenerateName(name);
        }


        private string RegenerateName(string n) =>
            Regex.IsMatch(n, @"\S+\((.+)?\)", RegexOptions.Compiled)
                ? n : GetFullName(n, Signature.ReturnType, Signature.Arguments);

        public static string GetFullName(string name, VeinComplexType returnType, IEnumerable<VeinArgumentRef> args)
            => $"{name}{new VeinMethodSignature(returnType, args.ToList()).ToTemplateString()}";

        public static string GetFullName(string name, VeinComplexType returnType, IEnumerable<VeinComplexType> args)
            => $"{name}{new VeinMethodSignature(returnType, args.Select((x, y) => new VeinArgumentRef(y.ToString(), x)).ToList()).ToTemplateString()}";

        public static string GetFullName(string name, VeinComplexType returnType, IEnumerable<VeinClass> args)
            => $"{name}{new VeinMethodSignature(returnType, args.Select((x, y) => new VeinArgumentRef(y.ToString(), x)).ToList()).ToTemplateString()}";

        public MethodFlags Flags { get; set; }

        public bool IsStatic => Flags.HasFlag(MethodFlags.Static);
        public bool IsPrivate => Flags.HasFlag(MethodFlags.Private);
        public bool IsExtern => Flags.HasFlag(MethodFlags.Extern);
        public bool IsAbstract => Flags.HasFlag(MethodFlags.Abstract);
        public bool IsVirtual => Flags.HasFlag(MethodFlags.Virtual);
        public bool IsOverride => !Flags.HasFlag(MethodFlags.Abstract) && Flags.HasFlag(MethodFlags.Override);
        public bool IsConstructor => RawName.Equals(METHOD_NAME_CONSTRUCTOR);
        public bool IsTypeConstructor => RawName.Equals("type_ctor");
        public bool IsDeconstructor => RawName.Equals(METHOD_NAME_DECONSTRUCTOR);
        public override bool IsSpecial => Flags.HasFlag(MethodFlags.Special);

        public sealed override string Name { get; protected set; }
        public string RawName => Name.Split('(').First();

        public override VeinMemberKind Kind => VeinMemberKind.Method;
    }
}
