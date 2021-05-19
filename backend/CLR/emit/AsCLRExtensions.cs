namespace wave.clr.emit
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Reflection.Emit;
    using runtime;

    public static class ManaTypeStorage
    {
        public static Dictionary<QualityTypeName, Type> Types = new();


        public static ModuleBuilder CLRModule;

        public static Type Intern(ManaType type)
        {
            if (CLRModule is null)
                throw new Exception($"Please set clr module.");
            if (Types.ContainsKey(type.FullName))
                return Types[type.FullName];

            var t = CLRModule.DefineType(type.FullName.NameWithNS.Replace("global::", "")
                    .Replace("/", "."), 
                type.classFlags.AsCLR(), Intern(type.Parent));


            foreach (var method in type.Members.OfType<ManaMethodBuilder>())
            {
                var clr_method = t.DefineMethod(method.RawName, method.Flags.AsCLR(), CallingConventions.Standard,
                    method.ReturnType.AsCLR(), method.Arguments.AsCLR());

                var gen = clr_method.GetILGenerator();

                gen.Emit(OpCodes.Ret);
            }

            foreach (var field in type.Members.OfType<ManaField>())
                t.DefineField(field.Name, field.FieldType.AsCLR(), field.Flags.AsCLR());
            Types.Add(type.FullName, t);
            return t;
        }
    }

    public static class AsCLRExtensions
    {
        public static Type[] AsCLR(this IEnumerable<ManaArgumentRef> args)
            => args.Select(x => x.Type.AsCLR()).ToArray();
        public static Type AsCLR(this ManaType type)
        {
            var result = Type.GetType(type.FullName.NameWithNS);
            if (result is null)
                return type.BakeCLRType();
            return result;
        }

        public static Type BakeCLRType(this ManaType type) => ManaTypeStorage.Intern(type);

        public static MethodAttributes AsCLR(this MethodFlags f)
        {
            var result = MethodAttributes.HideBySig | MethodAttributes.PrivateScope;
            var current = Enum.GetValues(typeof(MethodFlags))
                .Cast<MethodFlags>()
                .Where(v => f.HasFlag(v))
                .ToArray();
            foreach (var v in current)
            {
                switch (v)
                {
                    case MethodFlags.Public:
                        result |= MethodAttributes.Public;
                        break;
                    case MethodFlags.Static:
                        result |= MethodAttributes.Static;
                        break;
                    case MethodFlags.Internal:
                        break;
                    case MethodFlags.Protected:
                        result |= MethodAttributes.Family;
                        break;
                    case MethodFlags.Private:
                        result |= MethodAttributes.Private;
                        break;
                    case MethodFlags.Extern:
                        result |= MethodAttributes.PinvokeImpl;
                        break;
                    case MethodFlags.Virtual:
                        result |= MethodAttributes.Virtual;
                        break;
                    case MethodFlags.Abstract:
                        result |= MethodAttributes.Abstract;
                        break;
                    case MethodFlags.Override:
                        result |= MethodAttributes.Virtual;
                        break;
                    case MethodFlags.Special:
                        result |= MethodAttributes.SpecialName;
                        break;
                }
            }

            return result;
        }

        public static TypeAttributes AsCLR(this ClassFlags? f)
        {
            if (f is null)
                return TypeAttributes.Class;
            return f.AsCLR();
        }

        public static FieldAttributes AsCLR(this FieldFlags f)
        {
            FieldAttributes result = 0;

            var current = Enum.GetValues(typeof(FieldFlags))
                .Cast<FieldFlags>()
                .Where(v => f.HasFlag(v))
                .ToArray();

            foreach (var v in current)
            {
                switch (v)
                {
                    case FieldFlags.Literal:
                        result |= FieldAttributes.Literal;
                        break;
                    case FieldFlags.Public:
                        result |= FieldAttributes.Public;
                        break;
                    case FieldFlags.Static:
                        result |= FieldAttributes.Static;
                        break;
                    case FieldFlags.Protected:
                        result |= FieldAttributes.Family;
                        break;
                    case FieldFlags.Special:
                        result |= FieldAttributes.SpecialName;
                        break;
                    case FieldFlags.Readonly:
                        result |= FieldAttributes.InitOnly;
                        break;
                    case FieldFlags.Internal:
                        result |= FieldAttributes.Public;
                        break;
                }
            }

            return result;
        }

        public static TypeAttributes AsCLR(this ClassFlags f)
        {
            var result = 
                TypeAttributes.AnsiClass | 
                TypeAttributes.AutoLayout | 
                TypeAttributes.BeforeFieldInit;

            var current = Enum.GetValues(typeof(ClassFlags))
                .Cast<ClassFlags>()
                .Where(v => f.HasFlag(v))
                .ToArray();
            foreach (var v in current)
            {
                switch (v)
                {
                    case ClassFlags.Public:
                        result |= TypeAttributes.Public;
                        break;
                    case ClassFlags.Static:
                        result |= TypeAttributes.Sealed;
                        result |= TypeAttributes.Abstract;
                        break;
                    case ClassFlags.Internal:
                        result |= TypeAttributes.NestedPublic;
                        break;
                    case ClassFlags.Private:
                        result |= TypeAttributes.NestedPrivate;
                        break;
                    case ClassFlags.Abstract:
                        result |= TypeAttributes.Abstract;
                        break;
                    case ClassFlags.Special:
                        result |= TypeAttributes.SpecialName;
                        break;
                }
            }

            return result;
        }
    }
}