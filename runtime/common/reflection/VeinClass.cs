namespace vein.runtime
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;
    using collections;
    using extensions;
    using reflection;
    using static VeinTypeCode;

#if DEBUG
    public static class DebugRefenrence
    {
        private static ulong ID = 0;

        public static ulong Get() => Interlocked.Increment(ref ID);
    }
#endif

    [DebuggerDisplay("VeinClass {FullName}")]
    public class VeinClass : IEquatable<VeinClass>, IAspectable
    {
        public QualityTypeName FullName { get; set; }
        public string Name => FullName.Name;
        public string Alias => Aspects.FirstOrDefault(x => x.IsAlias())?.AsAlias()?.Name ?? Name;
        public string Path => FullName.Namespace;
        public virtual ClassFlags Flags { get; set; }
        public UniqueList<VeinClass> Parents { get; set; } = new();
        public List<VeinField> Fields { get; } = new();
        public List<VeinMethod> Methods { get; set; } = new();
        public VeinTypeCode TypeCode { get; set; } = TYPE_CLASS;
        public bool IsPrimitive => TypeCode is not TYPE_CLASS and not TYPE_NONE and not TYPE_STRING;
        public bool IsValueType => IsPrimitive || this.Walk(x => x.Name == "ValueType");
        public bool IsInterface => Flags.HasFlag(ClassFlags.Interface);
        public VeinModule Owner { get; set; }
        public List<Aspect> Aspects { get; } = new();
#if DEBUG
        public ulong ReferenceID = DebugRefenrence.Get();
#endif

        internal VeinClass(QualityTypeName name, VeinClass parent, VeinModule module)
        {
            this.FullName = name;
            if (parent is not null)
                this.Parents.Add(parent);
            this.Owner = module;
        }
        internal VeinClass(QualityTypeName name, VeinClass[] parents, VeinModule module)
        {
            this.FullName = name;
            this.Parents.AddRange(parents);
            this.Owner = module;
        }
        protected VeinClass() { }

        public bool IsSpecial => Flags.HasFlag(ClassFlags.Special);
        public bool IsPublic => Flags.HasFlag(ClassFlags.Public);
        public bool IsPrivate => Flags.HasFlag(ClassFlags.Private);
        public bool IsAbstract => Flags.HasFlag(ClassFlags.Abstract);
        public bool IsStatic => Flags.HasFlag(ClassFlags.Static);
        public bool IsInternal => Flags.HasFlag(ClassFlags.Internal);
        public bool IsAspect => Flags.HasFlag(ClassFlags.Aspect);

        public virtual VeinMethod GetDefaultDtor() => GetOrCreateTor("dtor");
        public virtual VeinMethod GetDefaultCtor() => GetOrCreateTor("ctor");

        public virtual VeinMethod GetStaticCtor() => GetOrCreateTor("type_ctor", true);


        protected virtual VeinMethod GetOrCreateTor(string name, bool isStatic = false)
            => Methods.FirstOrDefault(x => x.IsStatic == isStatic && x.RawName.Equals(name));

        public override string ToString()
            => $"{FullName}, {Flags}";


        public VeinMethod FindMethod(string name, IEnumerable<VeinClass> args_types) =>
            this.Methods.Concat(Parents.SelectMany(x => x.Methods))
                .FirstOrDefault(x =>
                {
                    var nameHas = x.RawName.Equals(name);
                    var args = x.Arguments.Where(NotThis).Select(z => z.Type).ToArray();
                    var argsHas = args.SequenceEqual(args_types);

                    if (nameHas && !argsHas)
                    {
                        argsHas = CheckInheritance(args_types.ToArray(), args);
                    }

                    return nameHas && argsHas;
                });

        // TODO
        private bool CheckInheritance(VeinClass[] current, VeinClass[] target)
        {
            if (current.Length != target.Length)
                return false;
            var result = true;
            for (int i = 0; i != current.Length; i++)
            {
                var t1 = current[i];
                var t2 = target[i];

                if (t1.FullName.Equals(t2.FullName))
                    continue;

                result &= t1.Parents.Any(x => x.FullName.Equals(t2.FullName));
            }

            return result;
        }

        public bool IsInner(VeinClass clazz)
        {
            if (Parents.Count == 0)
                return false;

            foreach (var parent in Parents)
            {
                if (parent.FullName == clazz.FullName)
                    return true;
                if (parent.IsInner(clazz))
                    return true;
            }

            return false;
        }

        public static bool NotThis(VeinArgumentRef arg) => !arg.Name.Equals(VeinArgumentRef.THIS_ARGUMENT);

        public VeinField FindField(string name) =>
            this.Fields.Concat(Parents.SelectMany(x => x.Fields))
                .FirstOrDefault(x => x.Name.Equals(name));


        public VeinMethod FindMethod(string name, Func<VeinMethod, bool> eq = null)
        {
            eq ??= s => s.RawName.Equals(name);

            foreach (var member in Methods)
            {
                if (eq(member))
                    return member;
            }

            return null;
        }

        public bool ContainsImpl(VeinMethod method)
        {
            foreach (var current in Methods)
            {
                if (current.Name == method.Name)
                    return current.IsOverride;
            }
            return false;
        }

        public bool Contains(VeinMethod method)
        {
            foreach (var current in Methods)
            {
                if (current.Name == method.Name)
                    return true;
            }
            return false;
        }

        #region Equality members

        public bool Equals(VeinClass other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(FullName, other.FullName);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((VeinClass)obj);
        }

        public override int GetHashCode()
            => HashCode.Combine(FullName);

        public static bool operator ==(VeinClass left, VeinClass right) => Equals(left, right);

        public static bool operator !=(VeinClass left, VeinClass right) => !Equals(left, right);

        #endregion
    }


    public static class TypeWalker
    {
        public static bool Walk(this VeinClass clazz, Func<VeinClass, bool> actor)
        {
            var target = clazz;

            while (target != null)
            {
                if (actor(target))
                    return true;

                if (!target.Parents.Any())
                    return false;

                foreach (var parent in target.Parents)
                {
                    // TODO
                    if (parent.IsInterface) continue;
                    target = parent;
                    break;
                }
            }
            return false;
        }
    }
}
