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
    public static class DebugReference
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
        public VeinModule Owner { get; set; }
        public List<Aspect> Aspects { get; } = new();
#if DEBUG
        public ulong ReferenceID = DebugReference.Get();
        protected VeinClass() => CreationPlace = Environment.StackTrace;

#else
        protected VeinClass()  {}
#endif
        public string CreationPlace { get; set; }
        internal VeinClass(QualityTypeName name, VeinClass parent, VeinModule module)
        {
            FullName = name;
            if (parent is not null)
                Parents.Add(parent);
            Owner = module;
            CreationPlace = Environment.StackTrace;
        }
        internal VeinClass(QualityTypeName name, VeinClass[] parents, VeinModule module)
        {
            FullName = name;
            Parents.AddRange(parents);
            Owner = module;
            CreationPlace = Environment.StackTrace;
        }

        

        public bool IsSpecial => Flags.HasFlag(ClassFlags.Special);
        public bool IsPublic => Flags.HasFlag(ClassFlags.Public);
        public bool IsPrivate => Flags.HasFlag(ClassFlags.Private);
        public bool IsAbstract => Flags.HasFlag(ClassFlags.Abstract);
        public bool IsStatic => Flags.HasFlag(ClassFlags.Static);
        public bool IsInternal => Flags.HasFlag(ClassFlags.Internal);
        public bool IsAspect => Flags.HasFlag(ClassFlags.Aspect);
        public bool IsPrimitive => TypeCode is not TYPE_CLASS and not TYPE_NONE and not TYPE_STRING;
        public bool IsValueType => IsPrimitive || this.Walk(x => x.Name == "ValueType");
        public bool IsInterface => Flags.HasFlag(ClassFlags.Interface);

        public virtual VeinMethod GetDefaultDtor() => GetOrCreateTor(VeinMethod.METHOD_NAME_DECONSTRUCTOR);
        public virtual VeinMethod GetDefaultCtor() => GetOrCreateTor(VeinMethod.METHOD_NAME_CONSTRUCTOR);

        public virtual VeinMethod GetStaticCtor() => GetOrCreateTor("type_ctor", true);


        protected virtual VeinMethod GetOrCreateTor(string name, bool isStatic = false)
            => Methods.FirstOrDefault(x => x.IsStatic == isStatic && x.RawName.Equals(name) && (x.IsDeconstructor || x.IsConstructor));

        public override string ToString()
            => $"{FullName}, {Flags}";


        public VeinMethod FindMethod(string name, IEnumerable<VeinComplexType> user_types, bool includeThis = false) =>
            Methods.Concat(Parents.SelectMany(x => x.Methods))
                .FirstOrDefault(x =>
                {
                    var userTypes = user_types.ToList();

                    var nameHas = x.RawName.Equals(name);

                    if (!nameHas)
                        return false;

                    var args = x.Signature.Arguments.Where(z => includeThis || NotThis(z)).Select(z => z.ComplexType).ToList();
                    var argsHas = CheckCompatibility(userTypes, args);

                    if (!argsHas && !args.Any(z => z.IsGeneric) && !userTypes.Any(z => z.IsGeneric))
                        argsHas = CheckInheritance(userTypes.Select(z => z.Class).ToArray(), args.Select(z => z.Class).ToArray());

                    return argsHas;
                });


        private bool CheckCompatibility(List<VeinComplexType> userArgs, List<VeinComplexType> methodArgs)
        {
            if (userArgs.Count != methodArgs.Count) return false;

            Dictionary<string, VeinClass> t2cMap = new();
            Dictionary<string, VeinTypeArg> t2tMap = new();

            for (int i = 0; i < userArgs.Count; i++)
            {
                var userType = userArgs[i];
                var methodType = methodArgs[i];

                if (methodType.IsGeneric)
                {
                    if (!CheckGenericCompatibility(userType, methodType, t2cMap, t2tMap))
                        return false;
                }
                else if (!CheckObjectCompatibility(methodType, userType))
                    return false;
            }
            return true;
        }

        private bool CheckGenericCompatibility(VeinComplexType userType, VeinComplexType methodType, Dictionary<string, VeinClass> genericMap, Dictionary<string, VeinTypeArg> generic2genericMap)
        {
            var genericName = methodType.TypeArg.Name;

            if (userType.IsGeneric)
            {
                if (generic2genericMap.TryGetValue(genericName, out var val))
                    return val.Name.Equals(genericName);
                generic2genericMap[genericName] = userType.TypeArg;
                return true;
            }

            if (genericMap.TryGetValue(genericName, out var value))
                return value.FullName.Equals(userType.Class.FullName);

            genericMap[genericName] = userType.Class;
            return true;
        }

        private bool CheckObjectCompatibility(VeinComplexType methodClass, VeinComplexType userClass)
        {
            if (methodClass.IsGeneric)
                return true;
            if (methodClass.Class.TypeCode == TYPE_OBJECT && userClass.IsGeneric)
                return true;
            return false;
        }

        private bool CheckCompatibilityV4(VeinComplexType[] userArgs, List<VeinComplexType> methodArgs)
        {
            if (userArgs.Length != methodArgs.Count) return false;

            Dictionary<string, VeinClass> genericMap = new();

            for (int i = 0; i < userArgs.Length; i++)
            {
                var userType = userArgs[i];
                var methodType = methodArgs[i];

                if (methodType.IsGeneric)
                {
                    var genericName = methodType.TypeArg.Name;

                    if (userType.IsGeneric)
                    {
                        if (!methodType.TypeArg.Name.Equals(userType.TypeArg.Name))
                            return false;
                    }
                    else
                    {
                        if (genericMap.TryGetValue(genericName, out var value))
                        {
                            if (!value.FullName.Equals(userType.Class.FullName) && userType.Class.TypeCode != TYPE_OBJECT)
                                return false;
                        }
                        else
                            genericMap[genericName] = userType.Class;
                    }
                }
                else
                {
                    if (userType.IsGeneric)
                        return false;
                    if (!methodType.Class.FullName.Equals(userType.Class.FullName) && methodType.Class.TypeCode != TYPE_OBJECT && userType.Class.TypeCode != TYPE_OBJECT)
                        return false;
                }
            }
            return true;
        }

        private bool CheckCompatibilityV3(List<VeinComplexType> userArgs, List<VeinComplexType> methodArgs)
        {
            if (userArgs.Count != methodArgs.Count) return false;

            var genericMap = new Dictionary<string, VeinClass>();

            for (int i = 0; i < userArgs.Count; i++)
            {
                var userType = userArgs[i];
                var methodType = methodArgs[i];

                if (methodType.IsGeneric)
                {
                    var genericName = methodType.TypeArg.Name;
                    if (userType.IsGeneric)
                    {
                        if (!methodType.TypeArg.Name.Equals(userType.TypeArg.Name))
                            return false;
                    }
                    else
                    {
                        if (genericMap.TryGetValue(genericName, out var value))
                        {
                            if (!value.FullName.Equals(userType.Class.FullName))
                                return false;
                        }
                        else
                            genericMap[genericName] = userType.Class;
                    }
                }
                else
                {
                    if (userType.IsGeneric)
                        return false;
                    if (!methodType.Class.FullName.Equals(userType.Class.FullName))
                        return false;
                }
            }
            return true;
        }


        private bool CheckCompatibilityV2(List<VeinComplexType> userArgs, List<VeinComplexType> methodArgs)
        {
            if (userArgs.Count != methodArgs.Count) return false;

            Dictionary<string, VeinClass> genericMap = new();

            for (int i = 0; i < userArgs.Count; i++)
            {
                var userType = userArgs[i];
                var methodType = methodArgs[i];

                if (methodType.IsGeneric)
                {
                    var genericName = methodType.TypeArg.Name;
                    if (genericMap.TryGetValue(genericName, out var value))
                    {
                        if (!value.FullName.Equals(userType.Class.FullName))
                            return false;
                    }
                    else
                        genericMap[genericName] = userType.Class;
                }
                else
                {
                    if (!methodType.Class.FullName.Equals(userType.Class.FullName))
                        return false;
                }
            }
            return true;
        }

        private bool CheckCompatibilityV1(List<VeinClass> userArgs, List<VeinComplexType> methodArgs)
        {
            if (userArgs.Count != methodArgs.Count) return false;

            Dictionary<string, VeinClass> genericMap = new();

            for (int i = 0; i < userArgs.Count; i++)
            {
                var userType = userArgs[i];
                var methodType = methodArgs[i];

                if (methodType.IsGeneric)
                {
                    var genericName = methodType.TypeArg.Name;
                    if (genericMap.TryAdd(genericName, userType)) continue;
                    if (!genericMap[genericName].FullName.Equals(userType.FullName))
                        return false;
                }
                else if (!methodType.Class.FullName.Equals(userType.FullName))
                    return false;
            }
            return true;
        }

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
            Fields.Concat(Parents.SelectMany(x => x.Fields))
                .FirstOrDefault(x => x.Name.Equals(name));

        public VeinProperty FindProperty(string name)
            => VeinProperty.RestoreFrom(name, this);


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
            if (obj.GetType() != GetType()) return false;
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
