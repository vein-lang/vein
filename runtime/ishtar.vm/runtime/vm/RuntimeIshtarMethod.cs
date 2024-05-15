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

        

        public static RuntimeMethodArgument* Create(IshtarTypes* types, (string name, VeinTypeCode code) data)
        {
            var a = IshtarGC.AllocateImmortal<RuntimeMethodArgument>();
            var (name, code) = data;
            
            *a = new RuntimeMethodArgument(types->ByTypeCode(code), StringStorage.Intern(name));

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


                lst->Add(a);
            }

            return lst;
        }

        public static DirectNativeList<RuntimeMethodArgument>* Create(VirtualMachine vm, VeinArgumentRef[] data)
        {
            if (data.Length == 0)
                return DirectNativeList<RuntimeMethodArgument>.New(1);
            
            var lst = DirectNativeList<RuntimeMethodArgument>.New(data.Length);

            foreach (var tuple in data)
            {
                var a = IshtarGC.AllocateImmortal<RuntimeMethodArgument>();
                
                *a = new RuntimeMethodArgument(vm.Vault.GlobalFindType(tuple.Type.FullName), StringStorage.Intern(tuple.Name));

                lst->Add(a);
            }

            return lst;
        }

        public static RuntimeMethodArgument* Create(IshtarTypes* types, string name, RuntimeIshtarClass* type)
        {
            var a = IshtarGC.AllocateImmortal<RuntimeMethodArgument>();

            *a = new RuntimeMethodArgument(type, StringStorage.Intern(name));

            return a;
        }

        public void ReplaceType(RuntimeIshtarClass* clazz)
        {
            VirtualMachine.Assert(clazz is not null, WNE.TYPE_LOAD, "[arg] Replacing type is nullptr");

            if (Type->IsUnresolved)
                Type = clazz;
        }

    }

    public unsafe struct RuntimeIshtarMethod : INamed
    {
        private readonly RuntimeIshtarMethod* _self;
        
        public MetaMethodHeader* Header;
        public PInvokeInfo PIInfo;

        public ulong vtable_offset;

        private readonly InternedString* _name;
        private readonly InternedString* _rawName;
        private readonly bool _ctor_called;

        public string Name => StringStorage.GetStringUnsafe(_name);
        public string RawName => StringStorage.GetStringUnsafe(_rawName);
        public MethodFlags Flags { get; private set; }
        public RuntimeIshtarClass* ReturnType { get; private set; }
        public RuntimeIshtarClass* Owner { get; private set; }
        public int ArgLength => Arguments->Length;

        public DirectNativeList<RuntimeMethodArgument>* Arguments { get; }
        public DirectNativeList<RuntimeAspect>* Aspects { get; }

        public void Assert(RuntimeIshtarMethod* @ref)
        {
            if (_self != @ref)
                throw new InvalidOperationException("GC moved unmovable memory!");
        }

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
            VirtualMachine.Assert(type is not null, WNE.TYPE_LOAD, "Replacing type is nullptr");
            VirtualMachine.Assert(ReturnType->IsUnresolved, WNE.TYPE_LOAD, "Replace returnType is possible only if type already unresolved");

            ReturnType = type;
        }


        internal RuntimeIshtarMethod(string name, MethodFlags flags, RuntimeIshtarClass* returnType, RuntimeIshtarClass* owner, RuntimeIshtarMethod* self,
            params RuntimeMethodArgument*[] args)
        {
            this = default;
            _self = self;
            Arguments =  DirectNativeList<RuntimeMethodArgument>.New(4);
            Aspects = DirectNativeList<RuntimeAspect>.New(4);
            _name = StringStorage.Intern(name);
            _rawName = StringStorage.Intern(name.Split('(').First());
            Flags = flags;
            ReturnType = returnType;
            Owner = owner;
            foreach (var argument in args)
                Arguments->Add(argument);
            _ctor_called = true;
        }

        internal RuntimeIshtarMethod(string name, MethodFlags flags, RuntimeIshtarClass* returnType, RuntimeIshtarClass* owner, RuntimeIshtarMethod* self,
            DirectNativeList<RuntimeMethodArgument>* args)
        {
            this = default;
            _self = self;
            Arguments = DirectNativeList<RuntimeMethodArgument>.New(4);
            Aspects = DirectNativeList<RuntimeAspect>.New(4);
            _name = StringStorage.Intern(name);
            _rawName = StringStorage.Intern(name.Split('(').First());
            Flags = flags;
            ReturnType = returnType;
            Owner = owner;
            if (args->Length != 0)
                Arguments->AddRange(args);
            _ctor_called = true;
        }

        public void SetILCode(uint* code, uint size)
        {
            if ((Flags & MethodFlags.Extern) != 0)
                throw new MethodHasExternException();
            if ((Flags & MethodFlags.Abstract) != 0)
                throw new MethodHasAbstractException();

            Header = IshtarGC.AllocateImmortal<MetaMethodHeader>();
            Header->code = code;
            Header->code_size = size;
        }

        public void SetExternalLink(void* @ref)
        {
            if ((Flags & MethodFlags.Extern) == 0)
                throw new MethodHasExternException();
            if ((Flags & MethodFlags.Abstract) == 0)
                throw new MethodHasAbstractException();
            PIInfo = new PInvokeInfo
            {
                Addr = @ref,
                iflags = 0
            };
        }


        public RuntimeIshtarMethod* AsNative(void* p)
        {
            this.PIInfo = PInvokeInfo.New(p);
            return _self;
        }
    }
}
