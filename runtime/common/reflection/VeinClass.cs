namespace vein.runtime
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using collections;
    using reflection;
    using static VeinTypeCode;
    

    [DebuggerDisplay("VeinClass {FullName}")]
    public record VeinClass : IAspectable
    {
        public virtual QualityTypeName FullName { get; set; }
        public NameSymbol Name => FullName.Name;
        public NamespaceSymbol Namespace => FullName.Namespace;
        public virtual ClassFlags Flags { get; set; }
        public UniqueList<VeinClass> Parents { get; set; } = new();
        public List<VeinField> Fields { get; } = new();
        public List<VeinMethod> Methods { get; set; } = new();
        public VeinTypeCode TypeCode { get; set; } = TYPE_CLASS;
        public VeinModule Owner { get; set; }
        public List<Aspect> Aspects { get; } = new();
        public List<VeinTypeArg> TypeArgs { get; init; } = new();
#if DEBUG
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
        public bool IsValueType => IsPrimitive || this.Walk(x => x.Name == NameSymbol.ValueType);
        public bool IsInterface => Flags.HasFlag(ClassFlags.Interface);
        public bool IsGenericType => TypeArgs.Any();

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
        public List<VeinMethod> FindAnyMethods(string name) =>
            Methods.Concat(Parents.SelectMany(x => x.Methods))
                .Where(x => {
                    var nameHas = x.RawName.Equals(name);

                    if (!nameHas)
                        return false;
                    return true;
                }).ToList();


        private bool CheckCompatibilityFunctionClass(VeinClass userArg, VeinClass methodArg)
        {
            var userInvoke = userArg.FindMethod("invoke");
            var methodInvoke = methodArg.FindMethod("invoke");

            if (userInvoke is null || methodInvoke is null)
                return false;

            return userInvoke.Signature.HasCompatibility(methodInvoke.Signature, true);
        }

        private bool CheckCompatibility(List<VeinComplexType> userArgs, List<VeinComplexType> methodArgs)
        {
            if (userArgs.Count != methodArgs.Count) return false;

            Dictionary<string, VeinClass> t2cMap = new();
            Dictionary<string, VeinTypeArg> t2tMap = new();

            for (int i = 0; i < userArgs.Count; i++)
            {
                var userType = userArgs[i];
                var methodType = methodArgs[i];

                if (!userType.IsGeneric && !methodType.IsGeneric && userType.Class == methodType.Class)
                    continue;

                if (methodType.IsGeneric)
                {
                    if (!CheckGenericCompatibility(userType, methodType, t2cMap, t2tMap))
                        return false;
                }
                else if (!CheckObjectCompatibility(methodType, userType))
                {
                    if (!CheckCompatibilityFunctionClass(userType, methodType))
                        return false;
                }
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

        private bool CheckInheritance(VeinClass[] current, VeinClass[] target)
        {
            if (current.Length != target.Length)
                return false;
            var result = true;
            for (int i = 0; i != current.Length; i++)
            {
                var t1 = current[i];
                var t2 = target[i];

                if (t1 is NullClass)
                    continue;

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


        public VeinClass ResolveGenericType(VeinComplexType type)
        {
            if (!IsGenericType)
                throw new InvalidOperationException();
            if (!TypeArgs.All(x => x is VeinDefinedTypeArg))
                throw new InvalidOperationException();

            var result = TypeArgs.OfType<VeinDefinedTypeArg>().FirstOrDefault(x => x.Name.Equals(type.TypeArg.Name));

            if (result is null)
                throw new InvalidOperationException();

            return result.definedClass;
        }

        public virtual bool Equals(VeinClass other) => FullName.Equals(other?.FullName);




        public VeinClass CreateAmorphousVersion(List<VeinClass> generics)
            => WithGenerics(this, generics);


        private static VeinClass WithGenerics(VeinClass clazz, List<VeinClass> generics)
        {
            if (clazz.TypeArgs.Count != generics.Count)
                throw new InvalidOperationException();

            var typeArgs = clazz.TypeArgs
                .Select((x, y) => x.AsDefined(generics[y]))
                .ToList<VeinTypeArg>();

            var newName = typeArgs
                .OfType<VeinDefinedTypeArg>()
                .Aggregate(clazz.FullName.Name.name,
                    (s, arg) => s.Replace(arg.Name, arg.definedClass.Name.name));

            return RegenerateGenerics(clazz with
            {
                Flags = clazz.Flags | ClassFlags.Amorphous,
                TypeArgs = typeArgs,
                FullName = clazz.FullName.OverrideName(new NameSymbol(newName)),
                Methods = clazz.Methods.Select(x => x.ShadowClone()).ToList(),
            });
        }

        private static VeinClass RegenerateGenerics(VeinClass clazz)
        {
            if (!clazz.Flags.HasFlag(ClassFlags.Amorphous))
                throw new InvalidOperationException();

            foreach (var method in clazz.Methods.Where(method => method.Signature.IsGeneric))
            {
                if (method.Signature.Arguments.Any(x => x.IsGeneric))
                {
                    var newArguments = new List<VeinArgumentRef>();
                    foreach (var argument in method.Signature.Arguments)
                    {
                        if (argument.IsThis)
                            newArguments.Add(new VeinArgumentRef(argument.Name, clazz));
                        else if (argument.IsGeneric)
                        {
                            var newGeneric = clazz.ResolveGenericType(argument.ComplexType);
                            newArguments.Add(new VeinArgumentRef(argument.Name, newGeneric));
                        }
                        else
                            newArguments.Add(argument);
                    }

                    method.Signature = method.Signature with
                    {
                        Arguments = newArguments
                    };
                }

                if (method.Signature.ReturnType.IsGeneric)
                {
                    var ret = clazz.ResolveGenericType(method.ReturnType);

                    method.Signature = method.Signature with
                    {
                        ReturnType = ret
                    };
                }
            }

            return clazz;
        }
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
