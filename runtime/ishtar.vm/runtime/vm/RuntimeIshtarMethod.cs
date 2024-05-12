namespace ishtar
{
    using vein.runtime;
    using runtime;
    using collections;
    using emit;

    public unsafe struct RuntimeMethodArgument(RuntimeIshtarClass* type, InternedString* name)
    {
        public const string THIS_ARGUMENT = "<this>";


        public RuntimeIshtarClass* Type { get; private set; } = type;
        public InternedString* Name { get; } = name;
        private void* _vein_arg_ref;


        public VeinArgumentRef Original
        {
            get => IshtarUnsafe.AsRef<VeinArgumentRef>(_vein_arg_ref);
            set => _vein_arg_ref = IshtarUnsafe.AsPointer(ref value);
        }


        public static RuntimeMethodArgument* Create(IshtarTypes* types, (string name, VeinTypeCode code) data)
        {
            var a = IshtarGC.AllocateImmortal<RuntimeMethodArgument>();
            var (name, code) = data;
            
            *a = new RuntimeMethodArgument(types->ByTypeCode(code), StringStorage.Intern(name));

            a->Original = new VeinArgumentRef(data.name, a->Type->Original);

            return a;
        }

        public static DirectNativeList<RuntimeMethodArgument>* Create(IshtarTypes* types, (string name, VeinTypeCode code)[] data)
        {
            var lst = DirectNativeList<RuntimeMethodArgument>.New(data.Length);

            foreach (var tuple in data)
            {
                var (name, code) = tuple;

                var a = IshtarGC.AllocateImmortal<RuntimeMethodArgument>();


                *a = new RuntimeMethodArgument(types->ByTypeCode(code), StringStorage.Intern(name));

                a->Original = new VeinArgumentRef(tuple.name, a->Type->Original);

                lst->Add(a);
            }

            return lst;
        }

        public static DirectNativeList<RuntimeMethodArgument>* Create(VirtualMachine vm, VeinArgumentRef[] data)
        {
            var lst = DirectNativeList<RuntimeMethodArgument>.New(data.Length);

            foreach (var tuple in data)
            {
                var a = IshtarGC.AllocateImmortal<RuntimeMethodArgument>();
                
                *a = new RuntimeMethodArgument(vm.Vault.GlobalFindType(tuple.Type.FullName), StringStorage.Intern(tuple.Name));

                a->Original = new VeinArgumentRef(tuple.Name, a->Type->Original);

                lst->Add(a);
            }

            return lst;
        }

        public static RuntimeMethodArgument* Create(IshtarTypes* types, string name, RuntimeIshtarClass* type)
        {
            var a = IshtarGC.AllocateImmortal<RuntimeMethodArgument>();

            *a = new RuntimeMethodArgument(type, StringStorage.Intern(name));

            a->Original = new VeinArgumentRef(name, a->Type->Original);

            return a;
        }

        public void ReplaceType(RuntimeIshtarClass* clazz)
        {
            if (Type->IsUnresolved)
                Type = clazz;
        }

    }

    public unsafe struct RuntimeIshtarMethod : INamed
    {
        private void* _veinMethodRef;

        public VeinMethod Original
        {
            get => IshtarUnsafe.AsRef<VeinMethod>(_veinMethodRef);
            set => _veinMethodRef = IshtarUnsafe.AsPointer(ref value);
        }

        public MetaMethodHeader* Header;
        public PInvokeInfo PIInfo;

        public ulong vtable_offset;

        public string Name => Original.Name;
        public string RawName => Original.RawName;
        public MethodFlags Flags => Original.Flags;
        public RuntimeIshtarClass* ReturnType { get; private set; }
        public RuntimeIshtarClass* Owner { get; private set; }
        public int ArgLength => Original.ArgLength;

        public DirectNativeList<RuntimeMethodArgument>* Arguments { get; } = DirectNativeList<RuntimeMethodArgument>.New(4);
        public DirectNativeList<RuntimeAspect>* Aspects { get; } = DirectNativeList<RuntimeAspect>.New(4);



        #region Flags

        public bool IsStatic => Flags.HasFlag(MethodFlags.Static);
        public bool IsPrivate => Flags.HasFlag(MethodFlags.Private);
        public bool IsExtern => Flags.HasFlag(MethodFlags.Extern);
        public bool IsAbstract => Flags.HasFlag(MethodFlags.Abstract);
        public bool IsVirtual => Flags.HasFlag(MethodFlags.Virtual);
        public bool IsOverride => !Flags.HasFlag(MethodFlags.Abstract) && Flags.HasFlag(MethodFlags.Override);
        public bool IsConstructor => RawName.Equals("ctor");
        public bool IsTypeConstructor => RawName.Equals("type_ctor");
        public bool IsDeconstructor => RawName.Equals("dtor");
        public bool IsSpecial => Flags.HasFlag(MethodFlags.Special);

        #endregion

        internal void ReplaceReturnType(RuntimeIshtarClass* type)
        {
            VirtualMachine.Assert(ReturnType->IsUnresolved, WNE.TYPE_LOAD, "Replace returnType is possible only if type already unresolved");
            
            ReturnType = type;
            Original.ReturnType = type->Original;
        }


        internal RuntimeIshtarMethod(string name, MethodFlags flags, RuntimeIshtarClass* returnType, RuntimeIshtarClass* owner,
            params RuntimeMethodArgument*[] args)
        {
            Original = new VeinMethod(name, flags, returnType->Original, owner->Original, Cast(args).ToArray())
            {
                Owner = owner->Original,
                ReturnType = returnType->Original
            };
            ReturnType = returnType;
            Owner = owner;
            foreach (var argument in args)
                Arguments->Add(argument);
        }

        internal RuntimeIshtarMethod(string name, MethodFlags flags, RuntimeIshtarClass* returnType, RuntimeIshtarClass* owner,
            DirectNativeList<RuntimeMethodArgument>* args)
        {
            Original = new VeinMethod(name, flags, returnType->Original, owner->Original, Cast(args).ToArray())
            {
                Owner = owner->Original,
                ReturnType = returnType->Original
            };
            ReturnType = returnType;
            Owner = owner;
            Arguments->AddRange(args);
        }

        private List<VeinArgumentRef> Cast(RuntimeMethodArgument*[] args)
        {
            var list = new List<VeinArgumentRef>();
            foreach (var argument in args)
                list.Add(argument->Original);

            return list;
        }

        private List<VeinArgumentRef> Cast(DirectNativeList<RuntimeMethodArgument>* args)
        {
            var list = new List<VeinArgumentRef>();

            args->ForEach(x =>
            {
                list.Add(x->Original);
            });
            
            return list;
        }

        public void SetILCode(uint* code, uint size)
        {
            if ((Original.Flags & MethodFlags.Extern) != 0)
                throw new MethodHasExternException();
            if ((Original.Flags & MethodFlags.Abstract) != 0)
                throw new MethodHasAbstractException();

            Header = IshtarGC.AllocateImmortal<MetaMethodHeader>();
            Header->code = code;
            Header->code_size = size;
        }

        public void SetExternalLink(void* @ref)
        {
            if ((Original.Flags & MethodFlags.Extern) == 0)
                throw new MethodHasExternException();
            if ((Original.Flags & MethodFlags.Abstract) == 0)
                throw new MethodHasAbstractException();
            PIInfo = new PInvokeInfo
            {
                Addr = @ref,
                iflags = 0
            };
        }


        public unsafe RuntimeIshtarMethod AsNative(void* p)
        {
            this.PIInfo = PInvokeInfo.New(p);
            return this;
        }
    }
}
