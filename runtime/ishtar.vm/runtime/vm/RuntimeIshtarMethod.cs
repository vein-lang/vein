namespace ishtar
{
    using vein.runtime;
    using runtime;
    using collections;
    using emit;
    using runtime.gc;
    using LLVMSharp;

    public unsafe struct RuntimeMethodArgument(
        RuntimeComplexType type,
        InternedString* name,
        RuntimeMethodArgument* self)
        : IEq<RuntimeMethodArgument>, IDisposable
    {
        public const string THIS_ARGUMENT = "<this>";


        public RuntimeComplexType Type { get; private set; } = type;
        public InternedString* Name { get; private set; } = name;
        public RuntimeMethodArgument* Self { get; private set; } = self;


        public void Dispose()
        {
            Type = default;
            Name = null;
            Self = null;
        }

        public static NativeList<RuntimeMethodArgument>* Create(IshtarTypes* types, (string name, VeinTypeCode code)[] data, void* parent)
        {
            var lst = IshtarGC.AllocateList<RuntimeMethodArgument>(parent, data.Length);

            foreach (var tuple in data)
            {
                var (name, code) = tuple;

                var a = IshtarGC.AllocateImmortal<RuntimeMethodArgument>(parent);


                *a = new RuntimeMethodArgument(types->ByTypeCode(code), StringStorage.Intern(name, a), a);


                lst->Add(a);
            }

            return lst;
        }

        public static NativeList<RuntimeMethodArgument>* Create(VirtualMachine vm, VeinArgumentRef[] data, void* parent)
        {
            if (data.Length == 0)
                return IshtarGC.AllocateList<RuntimeMethodArgument>(parent);
            
            var lst = IshtarGC.AllocateList<RuntimeMethodArgument>(parent, data.Length);

            foreach (var tuple in data)
            {
                var a = IshtarGC.AllocateImmortal<RuntimeMethodArgument>(parent);
                
                *a = new RuntimeMethodArgument(vm.Vault.GlobalFindType(tuple.Type.FullName.T(a)), StringStorage.Intern(tuple.Name, a), a);

                lst->Add(a);
            }

            return lst;
        }

        public static RuntimeMethodArgument* Create(IshtarTypes* types, string name, RuntimeComplexType type, void* parent)
        {
            var a = IshtarGC.AllocateImmortal<RuntimeMethodArgument>(parent);

            *a = new RuntimeMethodArgument(type, StringStorage.Intern(name, a), a);

            return a;
        }

        public void ReplaceType(RuntimeIshtarClass* clazz)
        {
            VirtualMachine.Assert(clazz is not null, WNE.TYPE_LOAD, "[arg] Replacing type is nullptr");
            if (Type.IsGeneric) return;
            if (Type.Class->IsUnresolved)
                Type = clazz;
        }

        public static bool Eq(RuntimeMethodArgument* p1, RuntimeMethodArgument* p2) => InternedString.Eq(p1->Name, p2->Name) && RuntimeIshtarClass.Eq(p1->Type, p2->Type);
    }
    
    public readonly unsafe struct RuntimeIshtarTypeArg(
        InternedString* id,
        InternedString* name,
        NativeList<IshtarParameterConstraint>* constraints)
        : IEq<RuntimeIshtarTypeArg>
    {
        public readonly InternedString* Id = id;
        public readonly InternedString* Name = name;
        public readonly NativeList<IshtarParameterConstraint>* Constraints = constraints;


        public static bool Eq(RuntimeIshtarTypeArg* p1, RuntimeIshtarTypeArg* p2)
        {
            if (p1 is null || p2 is null)
                throw null;
            return InternedString.Eq(p1->Id, p2->Id);
        }

        public static RuntimeIshtarTypeArg* Allocate(InternedString* Name,
            NativeList<IshtarParameterConstraint>* constraints, void* parent)
        {
            var type = IshtarGC.AllocateImmortal<RuntimeIshtarTypeArg>(parent);

            *type = new RuntimeIshtarTypeArg(CreateId(Name, constraints, type), Name, constraints);

            return type;
        }

        public static void Free(RuntimeIshtarTypeArg* typeArg)
            => IshtarGC.FreeImmortal(typeArg);


        // TODO
        private static InternedString* CreateId(InternedString* Name,
            NativeList<IshtarParameterConstraint>* constraints, RuntimeIshtarTypeArg* typeArg)
        {
            var rawName = StringStorage.GetStringUnsafe(Name);
            constraints->ForEach(x =>
            {
                if (x->Kind == VeinTypeParameterConstraint.BITTABLE)
                    rawName += "[bittable]";
                else if (x->Kind == VeinTypeParameterConstraint.CLASS)
                    rawName += "[class]";
                else if (x->Kind == VeinTypeParameterConstraint.SIGNATURE || x->Kind == VeinTypeParameterConstraint.TYPE)
                    rawName += $"[{x->Type->FullName->NameWithNS}]";
            });

            return StringStorage.Intern(rawName, typeArg);
        }
    }

    public unsafe struct IshtarParameterConstraint : IEquatable<IshtarParameterConstraint>, IEq<IshtarParameterConstraint>
    {
        public VeinTypeParameterConstraint Kind;
        public RuntimeIshtarClass* Type; // when TYPE or SIGNATURE

        public bool Equals(IshtarParameterConstraint other) => Kind == other.Kind && RuntimeIshtarClass.Eq(other.Type, Type);

        public static bool Eq(IshtarParameterConstraint* p1, IshtarParameterConstraint* p2)
        {
            if (p1 is null || p2 is null)
                throw null;
            return p1->Kind == p2->Kind && RuntimeIshtarClass.Eq(p1->Type, p2->Type);
        }

        public static IshtarParameterConstraint* CreateBittable(RuntimeIshtarModule* module)
        {
            var e = IshtarGC.AllocateImmortal<IshtarParameterConstraint>(module);
            *e = new IshtarParameterConstraint();
            e->Kind = VeinTypeParameterConstraint.BITTABLE;
            return e;
        }
        public static IshtarParameterConstraint* CreateClass(RuntimeIshtarModule* module)
        {
            var e = IshtarGC.AllocateImmortal<IshtarParameterConstraint>(module);
            *e = new IshtarParameterConstraint();
            e->Kind = VeinTypeParameterConstraint.CLASS;
            return e;
        }
        public static IshtarParameterConstraint* CreateSignature(RuntimeIshtarClass* @interface, RuntimeIshtarModule* module)
        {
            var e = IshtarGC.AllocateImmortal<IshtarParameterConstraint>(module);
            *e = new IshtarParameterConstraint();
            e->Kind = VeinTypeParameterConstraint.SIGNATURE;
            e->Type = @interface;
            return e;
        }

        public static IshtarParameterConstraint* CreateType(RuntimeIshtarClass* type, RuntimeIshtarModule* module)
        {
            var e = IshtarGC.AllocateImmortal<IshtarParameterConstraint>(module);
            *e = new IshtarParameterConstraint();
            e->Kind = VeinTypeParameterConstraint.TYPE;
            e->Type = type;
            return e;
        }
    }



    public readonly unsafe struct RuntimeComplexType
    {
        private readonly RuntimeIshtarTypeArg* _typeArg;
        private readonly RuntimeIshtarClass* _class;

        public RuntimeComplexType(RuntimeIshtarClass* @class) => _class = @class;

        public RuntimeComplexType(RuntimeIshtarTypeArg* typeArg) => _typeArg = typeArg;

        public bool IsGeneric => _typeArg is not null;

        public RuntimeIshtarTypeArg* TypeArg => _typeArg;
        public RuntimeIshtarClass* Class => _class;


        public static implicit operator RuntimeComplexType(RuntimeIshtarClass* cpx) => new(cpx);
        public static implicit operator RuntimeComplexType(RuntimeIshtarTypeArg* cpx) => new(cpx);


        public static implicit operator RuntimeIshtarClass*(RuntimeComplexType cpx)
        {
            if (cpx.IsGeneric)
                throw new NotSupportedException($"Trying summon non generic vein type, but complex type is generic");
            return cpx._class;
        }

        public static implicit operator RuntimeIshtarTypeArg*(RuntimeComplexType cpx)
        {
            if (cpx.IsGeneric)
                throw new NotSupportedException($"Trying summon generic type, but complex type is non generic vein type");
            return cpx._typeArg;
        }
    }

    public unsafe struct RuntimeIshtarSignature(
        RuntimeComplexType returnType,
        NativeList<RuntimeMethodArgument>* arguments)
    {
        public RuntimeComplexType ReturnType = returnType;
        public readonly NativeList<RuntimeMethodArgument>* Arguments = arguments;


        public static RuntimeIshtarSignature* Allocate(RuntimeComplexType returnType,
            NativeList<RuntimeMethodArgument>* arguments, void* parent)
        {
            var sig = IshtarGC.AllocateImmortal<RuntimeIshtarSignature>(parent);

            *sig = new RuntimeIshtarSignature(returnType, arguments);

            return sig;
        }

        public static void Free(RuntimeIshtarSignature* sig) => IshtarGC.FreeImmortal(sig);
    }

    [CTypeExport("ishtar_method_t")]
    public unsafe struct RuntimeIshtarMethod : INamed, IEq<RuntimeIshtarMethod>, IDisposable
    {
        private RuntimeIshtarMethod* _self;
        
        public MetaMethodHeader* Header;
        public PInvokeInfo PIInfo;

        public ulong vtable_offset;

        private readonly InternedString* _name;
        private readonly InternedString* _rawName;
        private readonly bool _ctor_called;

        public bool IsValid() => _name != null && Header != null && _rawName != null && _ctor_called;

        public string Name => StringStorage.GetStringUnsafe(_name);
        public string RawName => StringStorage.GetStringUnsafe(_rawName);
        public MethodFlags Flags { get; private set; }
        public RuntimeIshtarClass* ReturnType => Signature->ReturnType;
        public RuntimeIshtarClass* Owner { get; private set; }
        public int ArgLength => Arguments->Count;

        public NativeList<RuntimeMethodArgument>* Arguments => Signature->Arguments;
        public NativeList<RuntimeAspect>* Aspects { get; private set; }

        public RuntimeIshtarSignature* Signature { get; private set; }

        public void ForceSetAsAsync()
        {
            Flags |= MethodFlags.Async;
        }

        public void Dispose()
        {
            if (_self is null)
                return;
            DiagnosticDtorTraces[(nint)_self] = Environment.StackTrace;

            VirtualMachine.GlobalPrintln($"Disposed method '{Name}'");

            if (Header is not null)
                IshtarGC.FreeImmortal(Header);
            (Arguments)->ForEach(x => x->Dispose());
            (Arguments)->ForEach(IshtarGC.FreeImmortal);
            Aspects->Clear();
            (Arguments)->Clear();
            RuntimeIshtarSignature.Free(Signature);
            IshtarGC.FreeList(Aspects);
            IshtarGC.FreeList(Arguments);
            _self = null;
            Signature = null;
            Aspects = null;
        }

        public void Assert(RuntimeIshtarMethod* @ref)
        {
            if (_self != @ref)
                throw new InvalidOperationException("GC moved unmovable memory!");
        }

        public bool IsDisposed() => _self is null;

        #region Flags

        public bool IsStatic => Flags.HasFlag(MethodFlags.Static);
        public bool IsPrivate => Flags.HasFlag(MethodFlags.Private);
        public bool IsExtern => Flags.HasFlag(MethodFlags.Extern);
        public bool IsAbstract => Flags.HasFlag(MethodFlags.Abstract);
        public bool IsVirtual => Flags.HasFlag(MethodFlags.Virtual);
        public bool IsOverride => !Flags.HasFlag(MethodFlags.Abstract) && Flags.HasFlag(MethodFlags.Override);
        public bool IsConstructor => RawName.Equals("ctor");
        public bool IsTypeConstructor => RawName.Equals("type_ctor") || RawName.Equals("#type_ctor");
        public bool IsDeconstructor => RawName.Equals("dtor");
        public bool IsSpecial => Flags.HasFlag(MethodFlags.Special);

        #endregion

        internal void ReplaceReturnType(RuntimeIshtarClass* type)
        {
            VirtualMachine.Assert(type is not null, WNE.TYPE_LOAD, "Replacing type is nullptr");
            VirtualMachine.Assert((ReturnType)->IsUnresolved, WNE.TYPE_LOAD, "Replace returnType is possible only if type already unresolved");

            Signature->ReturnType = type;
        }

        private static readonly Dictionary<nint, string> DiagnosticCtorTraces = new();
        private static readonly Dictionary<nint, string> DiagnosticDtorTraces = new();

        private string DiagnosticCtorTrace => DiagnosticCtorTraces[(nint)_self];
        private string DiagnosticDtorTrace => DiagnosticDtorTraces[(nint)_self];

        internal RuntimeIshtarMethod(string name, MethodFlags flags, RuntimeComplexType returnType, RuntimeIshtarClass* owner, RuntimeIshtarMethod* self,
            params RuntimeMethodArgument*[] args)
        {
            this = default;
            _self = self;
            var arguments = IshtarGC.AllocateList<RuntimeMethodArgument>(self);
            Aspects = IshtarGC.AllocateList<RuntimeAspect>(self);
            _name = StringStorage.Intern(name, _self);
            _rawName = StringStorage.Intern(name.Split('(').First(), _self);
            Flags = flags;
            Owner = owner;
            foreach (var argument in args)
                arguments->Add(argument);
            _ctor_called = true;
            DiagnosticCtorTraces[(nint)self] = Environment.StackTrace;
            Signature = RuntimeIshtarSignature.Allocate(returnType, arguments, _self);
        }

        internal RuntimeIshtarMethod(string name, MethodFlags flags, RuntimeComplexType returnType, RuntimeIshtarClass* owner, RuntimeIshtarMethod* self,
            NativeList<RuntimeMethodArgument>* args)
        {
            this = default;
            _self = self;
            var arguments = IshtarGC.AllocateList<RuntimeMethodArgument>(self);
            Aspects = IshtarGC.AllocateList<RuntimeAspect>(self);
            _name = StringStorage.Intern(name, _self);
            _rawName = StringStorage.Intern(name.Split('(').First(), _self);
            Flags = flags;
            Owner = owner;
            if (args->Count != 0)
                arguments->AddRange(args);
            _ctor_called = true;
            DiagnosticCtorTraces[(nint)self] = Environment.StackTrace;
            Signature = RuntimeIshtarSignature.Allocate(returnType, arguments, _self);
        }

        public void SetILCode(uint* code, uint size)
        {
            if ((Flags & MethodFlags.Extern) != 0)
                throw new MethodHasExternException();
            if ((Flags & MethodFlags.Abstract) != 0)
                throw new MethodHasAbstractException();

            Header = IshtarGC.AllocateImmortal<MetaMethodHeader>(_self);
            Header->code = code;
            Header->code_size = size;
        }
        
        public RuntimeIshtarMethod* AsNative(void* p)
        {
            this.PIInfo = new PInvokeInfo()
            {
                isInternal = true,
                compiled_func_ref = (nint)p,
            };
            return _self;
        }

        public RuntimeIshtarMethod* AsNative(delegate*<stackval*, int, stackval> p)
        {
            this.PIInfo = new PInvokeInfo()
            {
                isInternal = true,
                compiled_func_ref = (nint)p,
            };
            return _self;
        }

        public static bool Eq(RuntimeIshtarMethod* p1, RuntimeIshtarMethod* p2)
        {
            if (p1->IsDisposed())
                return false;
            if (p2->IsDisposed())
                return false;
            return p1->Name.Equals(p2->Name) && RuntimeIshtarClass.Eq(p1->Owner, p2->Owner) &&
                   p1->ArgLength == p2->ArgLength;
        }



        // TODO remove fucking using .NET types

        public static string GetFullName(string name, RuntimeComplexType* returnType, IEnumerable<VeinArgumentRef> args)
        {
            static string ToTemplateString(RuntimeComplexType* t) => t->IsGeneric
                ? $"µ{StringStorage.GetStringUnsafe(t->TypeArg->Name)}"
                : t->Class->FullName->ToString();
            return $"{name}({string.Join(',', args.Select(x => $"{x.ToTemplateString()}"))}) -> {ToTemplateString(returnType)}";
        }

        public static string GetFullName(string name, RuntimeComplexType* returnType, NativeList<RuntimeMethodArgument>* args)
        {
            static string ToTemplateString(RuntimeComplexType* t) => t->IsGeneric
                ? $"µ{StringStorage.GetStringUnsafe(t->TypeArg->Name)}"
                : t->Class->FullName->ToString();

            var str = new List<string>();

            args->ForEach(x =>
            {
                var data = x->Type;
                str.Add(ToTemplateString(&data));
            });

            return $"{name}({string.Join(',', str)}) -> {ToTemplateString(returnType)}";
        }

        public static string GetFullName(string name, RuntimeIshtarClass* returnType, NativeList<RuntimeMethodArgument>* args)
        {
            static string ToTemplateString(RuntimeComplexType* t) => t->IsGeneric
                ? $"µ{StringStorage.GetStringUnsafe(t->TypeArg->Name)}"
                : t->Class->FullName->ToString();

            var str = new List<string>();

            args->ForEach(x =>
            {
                if (!NotThis(x))
                    return;

                var data = x->Type;
                str.Add(ToTemplateString(&data));
            });

            return $"{name}({string.Join(',', str)}) -> {returnType->FullName->ToString()}";
        }

        public static string GetFullName(string name, RuntimeIshtarClass* returnType, IEnumerable<VeinArgumentRef> args)
            => $"{name}({string.Join(',', args.Where(NotThis).Select(x => $"{x.ToTemplateString()}"))}) -> {returnType->FullName->ToString()}";

        public static string GetFullName(string name, RuntimeIshtarClass* returnType, RuntimeIshtarClass*[] args)
        {
            var stw = new List<string>();

            foreach (var @class in args)
                stw.Add($"{@class->FullName->ToString()}");

            return $"{name}({string.Join(',', stw)}) -> {returnType->FullName->ToString()}";
        }

        public static string GetFullName(string name, RuntimeComplexType* returnType, RuntimeIshtarClass*[] args)
        {
            static string ToTemplateString(RuntimeComplexType* t) => t->IsGeneric ?
                $"µ{StringStorage.GetStringUnsafe(t->TypeArg->Name)}" :
                t->Class->FullName->NameWithNS;

            var stw = new List<string>();

            foreach (var @class in args)
                stw.Add($"{@class->FullName->ToString()}");

            return $"{name}({string.Join(',', stw)}) -> {ToTemplateString(returnType)}";
        }

        private static bool NotThis(RuntimeMethodArgument* @ref) =>
            !StringStorage.GetStringUnsafe(@ref->Name).Equals(VeinArgumentRef.THIS_ARGUMENT);
        private static bool NotThis(VeinArgumentRef @ref) =>
            !@ref.Name.Equals(VeinArgumentRef.THIS_ARGUMENT);
        // end TODO
    }
}
