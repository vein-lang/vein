namespace ishtar
{
    using runtime;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using vein.collections;
    using vein.runtime;
    using collections;
    using static vein.runtime.VeinTypeCode;
    using static WNE;

    public interface ITransitionAlignment<in TKey, out TValue>
    {
        TValue this[TKey key] { get; }
    }

    public unsafe interface IUnsafeTransitionAlignment<in TKey, out TValue> where TValue : unmanaged
    {
        TValue* this[TKey key] { get; }
    }

    // WARNING: ALLOCATE ONLY BY GC
    public unsafe struct RuntimeIshtarClass :
        IUnsafeTransitionAlignment<string, RuntimeIshtarField>,
        IUnsafeTransitionAlignment<string, RuntimeIshtarMethod>
    {
        private readonly void* _veinClassRef;
        private readonly RuntimeIshtarClass* _selfReference;

        public DirectNativeList<RuntimeIshtarMethod>* Methods { get; } = DirectNativeList<RuntimeIshtarMethod>.New(16);
        public DirectNativeList<RuntimeIshtarField>* Fields { get; } = DirectNativeList<RuntimeIshtarField>.New(16);
        public DirectNativeList<RuntimeAspect>* Aspects { get; } = DirectNativeList<RuntimeAspect>.New(4);

        public VeinClass Original
        {
            get => IshtarUnsafe.AsRef<VeinClass>(_veinClassRef);
            private init => _veinClassRef = IshtarUnsafe.AsPointer(ref value);
        }

        public RuntimeIshtarModule* Owner { get; private set; }
        public RuntimeIshtarClass* Parent { get; private set; }
        public RuntimeQualityTypeName* FullName { get; private set; }

        public VeinTypeCode TypeCode
        {
            get => Original.TypeCode;
            set => Original.TypeCode = value;
        }
        public ClassFlags Flags => Original.Flags;
        public string Name => Original.Name;

        #region Flags

        public bool IsSpecial => Flags.HasFlag(ClassFlags.Special);
        public bool IsPublic => Flags.HasFlag(ClassFlags.Public);
        public bool IsPrivate => Flags.HasFlag(ClassFlags.Private);
        public bool IsAbstract => Flags.HasFlag(ClassFlags.Abstract);
        public bool IsStatic => Flags.HasFlag(ClassFlags.Static);
        public bool IsInternal => Flags.HasFlag(ClassFlags.Internal);
        public bool IsAspect => Flags.HasFlag(ClassFlags.Aspect);
        public bool IsPrimitive => TypeCode is not TYPE_CLASS and not TYPE_NONE and not TYPE_STRING;
        public bool IsValueType => Original.IsValueType;
        public bool IsUnresolved => Flags.HasFlag(ClassFlags.Unresolved);
        public bool IsInterface => Flags.HasFlag(ClassFlags.Interface);

        #endregion

        internal RuntimeIshtarClass(RuntimeQualityTypeName* name, RuntimeIshtarClass* parent, RuntimeIshtarModule* module)
        {
            if (module is null) return;
            ID = module->Vault->Value.TokenGranted.GrantClassID();
            runtime_token = new RuntimeToken(module->ID, ID);
            Original = new VeinClass((*name).T(), parent->Original, module->Original->Value);
            Owner = module;
            Parent = parent;
            FullName = name;
            fixed (RuntimeIshtarClass* p = &this)
                _selfReference = p;
        }

        internal void ReplaceParent(RuntimeIshtarClass* parent)
        {
            VirtualMachine.Assert(Parent->IsUnresolved, TYPE_LOAD, "Replace Parent is possible only if type already unresolved");

            Parent = parent;
            Original.Parents.Clear();
            Original.Parents.Add(parent->Original);
        }

        //internal RuntimeIshtarClass(RuntimeQualityTypeName* name, NativeList<RuntimeIshtarClass> parents, RuntimeIshtarModule* module)
        //{
        //    if (module is null) return;
        //    ID = module->Vault.TokenGranted.GrantClassID();
        //    runtime_token = new RuntimeToken(module->ID, ID);
        //    Original = new VeinClass((*name).T(), parents, module->Original);
        //}


        internal RuntimeIshtarField* DefineField(string name, FieldFlags flags, RuntimeIshtarClass* type)
        {
            var f = IshtarGC.AllocateImmortal<RuntimeIshtarField>();
            var fieldName = IshtarGC.AllocateImmortal<RuntimeFieldName>();

            *fieldName = new RuntimeFieldName(StringStorage.Intern(name));
            *f = new RuntimeIshtarField(_selfReference, fieldName, flags, type);
            this.Fields->Add(f);
            return f;
        }

        internal RuntimeIshtarField* DefineField(RuntimeFieldName* name, FieldFlags flags, RuntimeIshtarClass* type)
        {
            var f = IshtarGC.AllocateImmortal<RuntimeIshtarField>();
            *f = new RuntimeIshtarField(_selfReference, name, flags, type);
            this.Fields->Add(f);
            return f;
        }

        internal RuntimeIshtarMethod* DefineMethod(string name, RuntimeIshtarClass* returnType, MethodFlags flags, DirectNativeList<RuntimeMethodArgument>* args)
        {
            var method = IshtarGC.AllocateImmortal<RuntimeIshtarMethod>();

            *method = new RuntimeIshtarMethod(name, flags, returnType, _selfReference, args);

            Original.Methods.Add(method->Original);

//#if DEBUG
//            if (Methods.Any(x => x->Name.Equals(method->Name)))
//                throw new DuplicateItemException($"Method '{method->Name}' already exist in '{this.Original.Name}' class");
//#endif

            Methods->Add(method);
            return method;
        }

        internal RuntimeIshtarMethod* DefineMethod(string name, RuntimeIshtarClass* returnType, MethodFlags flags)
        {
            var method = IshtarGC.AllocateImmortal<RuntimeIshtarMethod>();

            *method = new RuntimeIshtarMethod(name, flags, returnType, _selfReference, DirectNativeList<RuntimeMethodArgument>.New(1));

            Original.Methods.Add(method->Original);

            //#if DEBUG
            //            if (Methods.Any(x => x->Name.Equals(method->Name)))
            //                throw new DuplicateItemException($"Method '{method->Name}' already exist in '{this.Original.Name}' class");
            //#endif

            Methods->Add(method);
            return method;
        }

        public RuntimeToken runtime_token { get; }
        public ushort ID { get; }

        public ulong computed_size = 0;
        public bool is_inited = false;
        public void** vtable = null;
        public ulong vtable_size = 0;
        
#if DEBUG_VTABLE
        public static Dictionary<ushort, debug_vtable> dvtables = new();


        public class debug_vtable
        {
            public RuntimeIshtarMethod*[] vtable_methods = null;
            public RuntimeIshtarField*[] vtable_fields = null;
            public string[] vtable_info = null;
            public ulong vtable_size = 0;
            public ulong computed_size = 0;
        }
#endif
        public void init_vtable(VirtualMachine vm)
        {
            if (is_inited) return;

            assertAddressNotMoved(vm);

            var dvtable = dvtables[ID] = new debug_vtable();

            if (TypeCode is TYPE_RAW)
            {
                computed_size = (ulong)sizeof(void*);
                vtable_size = (ulong)sizeof(void*);
                vtable = vm.GC.AllocVTable(1);
                is_inited = true;
                return;
            }

            if (Parent->IsUnresolved)
            {
                vm.FastFail(TYPE_MISMATCH, "Cannot init vtable when parent type is unresolved", vm.Frames.VTableFrame(_selfReference));
                return;
            }

            if (IsUnresolved)
            {
                vm.FastFail(TYPE_MISMATCH, "Cannot init vtable when type is unresolved", vm.Frames.VTableFrame(_selfReference));
                return;
            }

            computed_size = 0;
            
            if (Parent is not null)
            {
                Parent->init_vtable(vm);
                computed_size += Parent->computed_size;
#if DEBUG_VTABLE
                dvtable.computed_size += dvtables[Parent->ID].computed_size;
#endif
            }

            computed_size += (ulong)this.Methods->Length;
            computed_size += (ulong)this.Fields->Length;
            
#if DEBUG_VTABLE
            dvtable.computed_size += (ulong)this.Methods->Length;
            dvtable.computed_size += (ulong)this.Fields->Length;
#endif

            if (computed_size >= long.MaxValue) // fuck IntPtr ctor limit
            {
                vm.FastFail(TYPE_LOAD, $"'{FullName->ToString()}' too big object.", vm.Frames.VTableFrame(_selfReference));
                return;
            }

            if (computed_size == 0)
            {
                is_inited = true;
#if DEBUG_VTABLE
                dvtable.vtable_methods = [];
                dvtable.vtable_fields = [];
                dvtable.vtable_info = [];
#endif
                return;
            }

#if DEBUG_VTABLE
            dvtable.vtable_methods = new RuntimeIshtarMethod*[computed_size];
            dvtable.vtable_fields = new RuntimeIshtarField*[computed_size];
            dvtable.vtable_info = new string[computed_size];
#endif

            vtable = vm.GC.AllocVTable((uint)computed_size);
            
#if DEBUG_VTABLE
            dvtable.vtable_methods = new RuntimeIshtarMethod*[(long)computed_size];
            dvtable.vtable_fields = new RuntimeIshtarField*[(long)computed_size];
#endif
            var vtable_offset = 0UL;

            if (Parent is not null)
            {
                vtable_offset = Parent->computed_size;
                Unsafe.CopyBlock(vtable, Parent->vtable,
                    (uint)(sizeof(void*) * (uint)Parent->vtable_size));
#if DEBUG_VTABLE
                for (var i = 0ul; i != vtable_offset; i++)
                {
                    dvtable.vtable_methods[i] = dvtables[Parent->ID].vtable_methods[i];
                    dvtable.vtable_fields[i] = dvtables[Parent->ID].vtable_fields[i];
                }
#endif
            }

            for (var i = 0; i != Methods->Length; i++, vtable_offset++)
            {
                var method = (*Methods)[i];

                if ((method->Flags & MethodFlags.Abstract) != 0 && (Flags & ClassFlags.Abstract) == 0)
                {
                    vm.FastFail(TYPE_LOAD,
                        $"Method '{method->Name}' in '{Name}' type has invalid mapping.", vm.Frames.VTableFrame(_selfReference));
                    return;
                }

                vtable[vtable_offset] = method;
                method->vtable_offset = vtable_offset;

#if DEBUG_VTABLE
                dvtable.vtable_methods[vtable_offset] = method;
#endif

                if (Parent is null)
                    continue;
                {
                    var w = Parent->FindMethod(method->Name);
                    if (w == null && (method->Flags & MethodFlags.Override) != 0)
                        vm.FastFail(MISSING_METHOD,
                            $"Method '{method->Name}' mark as OVERRIDE," +
                            $" but parent class '{Parent->Name}'" +
                            $" no contained virtual/abstract method.", vm.Frames.VTableFrame(_selfReference));

                    if (w is null)
                        continue;

                    if ((method->Flags & MethodFlags.Override) != 0)
                    {
                        vtable[w->vtable_offset] = vtable[vtable_offset];
#if DEBUG_VTABLE
                        dvtable.vtable_methods[w->vtable_offset] = dvtable.vtable_methods[vtable_offset];
#endif
                    }
                }
            }

            if (Fields->Length != 0)
            {
                for (var i = 0; i != Fields->Length; i++, vtable_offset++)
                {
                    var field = (*Fields)[i];

                    if ((field->Flags & FieldFlags.Abstract) != 0 && (Flags & ClassFlags.Abstract) == 0)
                    {
                        vm.FastFail(TYPE_LOAD,
                            $"Field '{field->Name}' in '{this.Name}' type has invalid mapping.", vm.Frames.VTableFrame(_selfReference));
                        return;
                    }

                    vtable[vtable_offset] = get_field_default_value(field, vm);
                    field->vtable_offset = vtable_offset;

                    if (!field->FieldType->IsPrimitive)
                        Debug.Assert(vtable[vtable_offset] != null, $"Getting default value for '{field->FieldType->Name}' has incorrect");

#if DEBUG_VTABLE
                    dvtable.vtable_info[vtable_offset] = $"DEFAULT_VALUE OF [{field->FullName}::{field->FieldType->Name}]";
                    dvtable.vtable_fields[vtable_offset] = field;
#endif

                    if (Parent is null)
                        continue;

                    {
                        var w = Parent->FindField(field->FullName);

                        if (w == null && (field->Flags & FieldFlags.Override) != 0)
                            vm.FastFail(MISSING_FIELD,
                                $"Field '{field->Name}' mark as OVERRIDE," +
                                $" but parent class '{Parent->Name}' " +
                                $"no contained virtual/abstract method.", vm.Frames.VTableFrame(_selfReference));

                        if (w is null)
                            continue;

                        if ((field->Flags & FieldFlags.Override) != 0)
                        {
                            vtable[w->vtable_offset] = vtable[vtable_offset];
#if DEBUG_VTABLE
                            dvtable.vtable_fields[w->vtable_offset] = dvtable.vtable_fields[vtable_offset];
#endif
                        }
                    }
                }
            }

            if (Fields->Length != 0) for (var i = 0; i != Fields->Length; i++)
                (*Fields)[i]->init_mapping(vm.Frames.VTableFrame(_selfReference));

            is_inited = true;
            if (Parent is null)
            {
                vtable_size = computed_size;
#if DEBUG_VTABLE
                dvtable.vtable_size = computed_size;
#endif
            }
            else
            {
                vtable_size = computed_size - Parent->computed_size;
#if DEBUG_VTABLE
                dvtable.vtable_size = dvtable.computed_size - Parent->computed_size;
#endif
            }
        }

        
        public IshtarObject* get_field_default_value(RuntimeIshtarField* field, VirtualMachine vm)
        {
            assertAddressNotMoved(vm);

            if (field->default_value != null)
                return field->default_value;
            var frame = vm.Frames.VTableFrame(_selfReference);

            if (field->FieldType->IsPrimitive)
            {
                var defaultValue = stackval.Allocate(frame, 1);
                
                vm.GC.UnsafeAllocValueInto(field->FieldType, defaultValue.Ref);

                return IshtarMarshal.Boxing(frame, defaultValue.Ref);
            }
            return field->default_value = vm.GC.AllocObject(field->FieldType, frame);
        }
        public RuntimeIshtarField* FindField(string name)
        {
            var field = Fields->FirstOrNull(x => x->Name.Equals(name));

            if (field is not null)
                return field;

            if (Parent is null)
                return null;

            return Parent->FindField(name);
        }

        public RuntimeIshtarMethod* FindMethod(string fullyName)
        {
            var method = Methods
                ->FirstOrNull(x => x->RawName.Equals(fullyName) || x->Name.Equals(fullyName));

            if (method is not null)
                return method;

            if (Parent is null)
                return null;

            return Parent->FindMethod(fullyName);
        }

        public RuntimeIshtarMethod* FindMethod(string fullyName, UnsafePredicate<RuntimeIshtarMethod> predicate)
        {
            var method = Methods
                ->FirstOrNull(x => (x->RawName.Equals(fullyName) || x->Name.Equals(fullyName)) || predicate(x));

            if (method is not null)
                return method;

            if (Parent is null)
                return null;

            return Parent->FindMethod(fullyName, predicate);
        }

        public RuntimeIshtarMethod* GetDefaultDtor() => GetDefaultDtor();
        public RuntimeIshtarMethod* GetDefaultCtor() => GetDefaultCtor();
        
        public IUnsafeTransitionAlignment<string, RuntimeIshtarField> Field => this;
        public IUnsafeTransitionAlignment<string, RuntimeIshtarMethod> Method => this;

        RuntimeIshtarField* IUnsafeTransitionAlignment<string, RuntimeIshtarField>.this[string key]
            => FindField(key);
        RuntimeIshtarMethod* IUnsafeTransitionAlignment<string, RuntimeIshtarMethod>.this[string key]
            => FindMethod(key);

        public bool IsInner(RuntimeIshtarClass* clazz)
        {
            if (Parent is null)
                return false;

            if (Parent->FullName == clazz->FullName)
                return true;
            if (Parent->IsInner(clazz))
                return true;
            return false;
        }

        [Conditional("DEBUG_VTABLE")]
        private void assertAddressNotMoved(VirtualMachine vm)
        {
            fixed (RuntimeIshtarClass* p = &this)
                vm.Frames.VTableFrame(_selfReference).assert(p == _selfReference, GC_MOVED_UNMOVABLE_MEMORY, $"For class '{Name}' pin pointer is incorrect, maybe GC moved memory");
        }
    }
}
