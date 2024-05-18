namespace ishtar
{
    using vein.runtime;
    using runtime;
    using collections;
    using emit;

    public unsafe struct RuntimeMethodArgument(RuntimeIshtarClass* type, InternedString* name, RuntimeMethodArgument* self) : IEq<RuntimeMethodArgument>, IDisposable
    {
        public const string THIS_ARGUMENT = "<this>";


        public RuntimeIshtarClass* Type { get; private set; } = type;
        public InternedString* Name { get; } = name;
        public RuntimeMethodArgument* Self { get; } = self;


        public void Dispose()
        {
            type = null;
            name = null;
            IshtarGC.FreeImmortal(self);
        }


        public static RuntimeMethodArgument* Create(IshtarTypes* types, (string name, VeinTypeCode code) data)
        {
            var a = IshtarGC.AllocateImmortal<RuntimeMethodArgument>();
            var (name, code) = data;
            
            *a = new RuntimeMethodArgument(types->ByTypeCode(code), StringStorage.Intern(name), a);

            return a;
        }

        public static NativeList<RuntimeMethodArgument>* Create(IshtarTypes* types, (string name, VeinTypeCode code)[] data)
        {
            var lst = IshtarGC.AllocateList<RuntimeMethodArgument>(data.Length);

            foreach (var tuple in data)
            {
                var (name, code) = tuple;

                var a = IshtarGC.AllocateImmortal<RuntimeMethodArgument>();


                *a = new RuntimeMethodArgument(types->ByTypeCode(code), StringStorage.Intern(name), a);


                lst->Add(a);
            }

            return lst;
        }

        public static NativeList<RuntimeMethodArgument>* Create(VirtualMachine vm, VeinArgumentRef[] data)
        {
            if (data.Length == 0)
                return IshtarGC.AllocateList<RuntimeMethodArgument>();
            
            var lst = IshtarGC.AllocateList<RuntimeMethodArgument>(data.Length);

            foreach (var tuple in data)
            {
                var a = IshtarGC.AllocateImmortal<RuntimeMethodArgument>();
                
                *a = new RuntimeMethodArgument(vm.Vault.GlobalFindType(tuple.Type.FullName.T()), StringStorage.Intern(tuple.Name), a);

                lst->Add(a);
            }

            return lst;
        }

        public static RuntimeMethodArgument* Create(IshtarTypes* types, string name, RuntimeIshtarClass* type)
        {
            var a = IshtarGC.AllocateImmortal<RuntimeMethodArgument>();

            *a = new RuntimeMethodArgument(type, StringStorage.Intern(name), a);

            return a;
        }

        public void ReplaceType(RuntimeIshtarClass* clazz)
        {
            VirtualMachine.Assert(clazz is not null, WNE.TYPE_LOAD, "[arg] Replacing type is nullptr");

            if (Type->IsUnresolved)
                Type = clazz;
        }

        public static bool Eq(RuntimeMethodArgument* p1, RuntimeMethodArgument* p2) => InternedString.Eq(p1->Name, p2->Name) && RuntimeIshtarClass.Eq(p1->Type, p2->Type);
    }

    public unsafe struct RuntimeIshtarMethod : INamed, IEq<RuntimeIshtarMethod>, IDisposable
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
        public int ArgLength => Arguments->Count;

        public NativeList<RuntimeMethodArgument>* Arguments { get; }
        public NativeList<RuntimeAspect>* Aspects { get; }


        public void Dispose()
        {
            VirtualMachine.GlobalPrintln($"Disposed method '{Name}'");

            if (Header is not null)
                IshtarGC.FreeImmortal(Header);
            Arguments->ForEach(x => x->Dispose());
            Aspects->ForEach(x => x->Dispose());
            IshtarGC.FreeList(Arguments);
            IshtarGC.FreeImmortal(_self);
        }

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
        public bool IsTypeConstructor => RawName.Equals("type_ctor") || RawName.Equals("#type_ctor");
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
            Arguments =  IshtarGC.AllocateList<RuntimeMethodArgument>();
            Aspects = IshtarGC.AllocateList<RuntimeAspect>();
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
            NativeList<RuntimeMethodArgument>* args)
        {
            this = default;
            _self = self;
            Arguments = IshtarGC.AllocateList<RuntimeMethodArgument>();
            Aspects = IshtarGC.AllocateList<RuntimeAspect>();
            _name = StringStorage.Intern(name);
            _rawName = StringStorage.Intern(name.Split('(').First());
            Flags = flags;
            ReturnType = returnType;
            Owner = owner;
            if (args->Count != 0)
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

        public static bool Eq(RuntimeIshtarMethod* p1, RuntimeIshtarMethod* p2) => p1->Name.Equals(p2->Name) && RuntimeIshtarClass.Eq(p1->Owner, p2->Owner) && p1->ArgLength == p2->ArgLength;
    }
}
