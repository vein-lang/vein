namespace ishtar
{
    using runtime;
    using vein.collections;
    using vein.runtime;
    using collections;
    using vein.exceptions;
    using static vein.runtime.VeinTypeCode;
    using static WNE;
    using runtime.gc;


    // ReSharper disable once TypeParameterCanBeVariant
    public interface ITransitionAlignment<TKey, TValue>
    {
        TValue this[TKey key] { get; }
    }

    // ReSharper disable once TypeParameterCanBeVariant
    public unsafe interface IUnsafeTransitionAlignment<TKey, TValue> where TValue : unmanaged
    {
        TValue* this[TKey key] { get; }
    }

    // WARNING: ALLOCATE ONLY BY GC
    [DebuggerDisplay("name = {_debug_name}, id = {ID}, isValid: {IsValid}")]
    [CTypeExport("ishtar_class_t")]
    public unsafe struct RuntimeIshtarClass :
        IUnsafeTransitionAlignment<string, RuntimeIshtarField>,
        IUnsafeTransitionAlignment<string, RuntimeIshtarMethod>, IEq<RuntimeIshtarClass>, IDisposable
    {
        private RuntimeIshtarClass* _selfReference;

        [field: CTypeOverride("void*")]
        public NativeList<RuntimeIshtarMethod>* Methods { get; private set; }
        [field: CTypeOverride("void*")]
        public NativeList<RuntimeIshtarField>* Fields { get; private set; }
        [field: CTypeOverride("void*")]
        public NativeList<RuntimeAspect>* Aspects { get; private set; } 
        
        public RuntimeIshtarModule* Owner { get; private set; }
        public RuntimeIshtarClass* Parent { get; private set; }
        public RuntimeQualityTypeName* FullName { get; private set; }

        private bool _isDisposed;

        private string _debug_name => FullName->NameWithNS;

        public VeinTypeCode TypeCode { get; set; }
        public ClassFlags Flags { get; set; }
        public string Name => FullName->Name;

        public RuntimeToken runtime_token { get; }
        public uint ID { get; }

        [CIfDefined("DEBUG")]
        public ushort Magic1;
        [CIfDefined("DEBUG")]
        public ushort Magic2;

        public bool IsValid => Magic1 == 45 && Magic2 == 75;

        public ulong computed_size = 0;
        public bool is_inited = false;
        public void** vtable = null;
        public ulong vtable_size = 0;

        public void Dispose()
        {
            if (_isDisposed) return;
            if (FullName is null)
                return;
            var name = Name;
            VirtualMachine.GlobalPrintln($"@@@ Disposed class '{name}'");
            Methods->ForEach(x => x->Dispose());
            Methods->ForEach(IshtarGC.FreeImmortal);
            Methods->Clear();


            Fields->ForEach(x => x->Dispose());
            Fields->ForEach(IshtarGC.FreeImmortal);
            
            Methods->Clear();
            Fields->Clear();
            Aspects->Clear();

            IshtarGC.FreeList(Methods);
            IshtarGC.FreeList(Fields);
            IshtarGC.FreeList(Aspects);

            Methods = null;
            Fields = null;
            Aspects = null;

            Owner = null;
            Parent = null;
            FullName = null;
            _selfReference = null;
            VirtualMachine.GlobalPrintln($"@@@ end dispose class '{name}'");
            _isDisposed = true;
        }

        
        #region Flags

        public bool IsSpecial => Flags.HasFlag(ClassFlags.Special);
        public bool IsPublic => Flags.HasFlag(ClassFlags.Public);
        public bool IsPrivate => Flags.HasFlag(ClassFlags.Private);
        public bool IsAbstract => Flags.HasFlag(ClassFlags.Abstract);
        public bool IsStatic => Flags.HasFlag(ClassFlags.Static);
        public bool IsInternal => Flags.HasFlag(ClassFlags.Internal);
        public bool IsAspect => Flags.HasFlag(ClassFlags.Aspect);
        public bool IsPrimitive => TypeCode is not TYPE_CLASS and not TYPE_NONE and not TYPE_STRING;
        public bool IsValueType => IsPrimitive || Walk(x => x->Name == "ValueType");
        public bool IsUnresolved => Flags.HasFlag(ClassFlags.Unresolved);
        public bool IsInterface => Flags.HasFlag(ClassFlags.Interface);

        #endregion

        // TODO
        public bool Walk(UnsafeFilter_Delegate<RuntimeIshtarClass> actor)
        {
            var target = _selfReference;
            while (target != null)
            {
                if (actor(target))
                    return true;

                if (target->Parent is null)
                    return false;

                if (target->Parent->IsInterface) continue;
                target = target->Parent;
            }
            return false;
        }

        internal RuntimeIshtarClass(RuntimeQualityTypeName* name, RuntimeIshtarClass* parent, RuntimeIshtarModule* module, RuntimeIshtarClass* self)
        {
            Methods = IshtarGC.AllocateList<RuntimeIshtarMethod>(_selfReference);
            Fields = IshtarGC.AllocateList<RuntimeIshtarField>(_selfReference);
            Aspects = IshtarGC.AllocateList<RuntimeAspect>(_selfReference);
            Magic1 = 45;
            Magic2 = 75;
            _selfReference = self;
            FullName = name;
            Owner = module;
            Parent = parent;
            if (module is null) return;
            ID = module->Vault->Value.TokenGranted.GrantClassID();
            runtime_token = new RuntimeToken(module->ID, ID);
        }

        internal void ReplaceParent(RuntimeIshtarClass* parent)
        {
            VirtualMachine.Assert(Parent->IsUnresolved, TYPE_LOAD, "Replace Parent is possible only if type already unresolved");
            Parent = parent;
        }
        

        internal RuntimeIshtarField* DefineField(string name, FieldFlags flags, RuntimeIshtarClass* type)
        {
            var exist = Fields->FirstOrNull(x => x->Name.Equals(name));

            if (exist is not null)
                return exist;

            var f = IshtarGC.AllocateImmortal<RuntimeIshtarField>(_selfReference);
            var fieldName = IshtarGC.AllocateImmortal<RuntimeFieldName>(_selfReference);

            *fieldName = new RuntimeFieldName(StringStorage.Intern(name, f));
            *f = new RuntimeIshtarField(_selfReference, fieldName, flags, type, f);
            Fields->Add(f);
            return f;
        }

        internal RuntimeIshtarField* DefineField(RuntimeFieldName* name, FieldFlags flags, RuntimeComplexType type)
        {
            var exist = Fields->FirstOrNull(x => x->Name.Equals(name->Name));

            if (exist is not null)
                return exist;

            var f = IshtarGC.AllocateImmortal<RuntimeIshtarField>(_selfReference);
            *f = new RuntimeIshtarField(_selfReference, name, flags, type, f);
            Fields->Add(f);
            return f;
        }

        internal RuntimeIshtarMethod* DefineMethod(string name, RuntimeComplexType returnType, MethodFlags flags, NativeList<RuntimeMethodArgument>* args)
        {
            var method = IshtarGC.AllocateImmortal<RuntimeIshtarMethod>(_selfReference);
            *method = new RuntimeIshtarMethod(name, flags, returnType, _selfReference, method, args);
            method->Assert(method);
            var exist = Methods->FirstOrNull(x =>
            {
                if (x->IsDisposed())
                    return false;
                x->Assert(x);
                return x->Name.Equals(name);
            });


            if (method->IsConstructor && exist is not null)
                Methods->Swap(exist, method);
            if (method->IsTypeConstructor && exist is not null)
                Methods->Swap(exist, method);
            if (exist is not null && method->Header is not null)
                throw new MethodAlreadyDefined($"Method '{exist->Name}' already defined in '{Name}' class");
            if (exist is not null && method->Header is null)
            {
                Methods->Remove(method);
                method->Dispose();
                return exist;
            }
            Methods->Add(method);
            return method;
        }

        internal RuntimeIshtarMethod* DefineMethod(string name, RuntimeIshtarClass* returnType, MethodFlags flags)
        {
            var method = IshtarGC.AllocateImmortal<RuntimeIshtarMethod>(_selfReference);

            *method = new RuntimeIshtarMethod(name, flags, returnType, _selfReference, method, IshtarGC.AllocateList<RuntimeMethodArgument>(_selfReference));

            Methods->Add(method);
            return method;
        }

#if DEBUG_VTABLE
        private static readonly Dictionary<uint, debug_vtable> dvtables = new();

        public debug_vtable dvtable => dvtables[ID];


        public class debug_vtable
        {
            public RuntimeIshtarMethod*[] vtable_methods = null;
            public RuntimeIshtarField*[] vtable_fields = null;
            public string[] vtable_info = null;
            public ulong vtable_size = 0;
            public ulong computed_size = 0;
        }
#endif
        private readonly struct native_disposer<T>(T* ptr, UnsafeVoid_Delegate<T> disposer) : IDisposable where T : unmanaged
        {
            public void Dispose() => disposer(ptr);
        }
        
        public void init_vtable(VirtualMachine* vm, CallFrame* fr = null)
        {
            if (is_inited) return;

            var typeCtor = FindMethod("#type()", false);
            if (typeCtor is null)
                typeCtor = DefineMethod("#type()", _selfReference, MethodFlags.Special | MethodFlags.Static);
            CallFrame* frame;
            if (fr is null)
                frame = CallFrame.Create(typeCtor, null);
            else
                frame = CallFrame.Create(typeCtor, fr);

            using var _ = new native_disposer<CallFrame>(frame, x => x->Dispose());
            
            var dvtable = dvtables[ID] = new debug_vtable();

            if (TypeCode is TYPE_RAW)
            {
                computed_size = (ulong)sizeof(void*);
                vtable_size = (ulong)sizeof(void*);
                vtable = vm->gc->AllocVTable(1);
                is_inited = true;
                return;
            }

            
            computed_size = 0;
            
            if (Parent is not null)
            {
                if (Parent->IsUnresolved)
                {
                    vm->FastFail(TYPE_MISMATCH, "Cannot init vtable when parent type is unresolved", frame);
                    return;
                }

                if (IsUnresolved)
                {
                    vm->FastFail(TYPE_MISMATCH, "Cannot init vtable when type is unresolved", frame);
                    return;
                }

                Parent->init_vtable(vm, fr);
                computed_size += Parent->computed_size;
#if DEBUG_VTABLE
                dvtable.computed_size += dvtables[Parent->ID].computed_size;
#endif
            }

            computed_size += (ulong)this.Methods->Count;
            computed_size += (ulong)this.Fields->Count;
            
#if DEBUG_VTABLE
            dvtable.computed_size += (ulong)this.Methods->Count;
            dvtable.computed_size += (ulong)this.Fields->Count;
#endif

            if (computed_size >= long.MaxValue) // fuck IntPtr ctor limit
            {
                vm->FastFail(TYPE_LOAD, $"'{FullName->ToString()}' too big object.", frame);
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

            vtable = vm->gc->AllocVTable((uint)computed_size);
            
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

            for (var i = 0; i != Methods->Count; i++, vtable_offset++)
            {
                var method = Methods->Get(i);

                if ((method->Flags & MethodFlags.Abstract) != 0 && (Flags & ClassFlags.Abstract) == 0)
                {
                    vm->FastFail(TYPE_LOAD,
                        $"Method '{method->Name}' in '{Name}' type has invalid mapping.", frame);
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
                        vm->FastFail(MISSING_METHOD,
                            $"Method '{method->Name}' mark as OVERRIDE," +
                            $" but parent class '{Parent->Name}'" +
                            $" no contained virtual/abstract method.", frame);

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

            if (Fields->Count != 0)
            {
                for (var i = 0; i != Fields->Count; i++, vtable_offset++)
                {
                    var field = Fields->Get(i);

                    if ((field->Flags & FieldFlags.Abstract) != 0 && (Flags & ClassFlags.Abstract) == 0)
                    {
                        vm->FastFail(TYPE_LOAD,
                            $"Field '{field->Name}' in '{this.Name}' type has invalid mapping.", frame);
                        return;
                    }

                    // todo literal is not support
                    if (!field->IsLiteral)
                    {
                        field->vtable_offset = vtable_offset;
                        vtable[vtable_offset] = null;
                        //if (!field->FieldType->IsPrimitive)
                        //    vtable[vtable_offset] = get_field_default_value(field, vm);
                        //else
                        //    vtable[vtable_offset] = null; // TODO literal support
                        //field->vtable_offset = vtable_offset;

                        //if (!field->FieldType->IsPrimitive)
                        //    Debug.Assert(vtable[vtable_offset] != null, $"Getting default value for '{field->FieldType->Name}' has incorrect");
                    }
                    else
                        field->vtable_offset = vtable_offset;


#if DEBUG_VTABLE
                    if (field->FieldType.IsGeneric)
                        dvtable.vtable_info[vtable_offset] = $"DEFAULT_VALUE OF [{field->FullName->ToString()}::{StringStorage.GetStringUnsafe(field->FieldType.TypeArg->Name)}]";
                    else
                        dvtable.vtable_info[vtable_offset] = $"DEFAULT_VALUE OF [{field->FullName->ToString()}::{field->FieldType.Class->FullName->ToString()}]";
                    dvtable.vtable_fields[vtable_offset] = field;
#endif

                    if (Parent is null)
                        continue;

                    {
                        var w = Parent->FindField(field->FullName);

                        if (w == null && (field->Flags & FieldFlags.Override) != 0)
                            vm->FastFail(MISSING_FIELD,
                                $"Field '{field->Name}' mark as OVERRIDE," +
                                $" but parent class '{Parent->Name}' " +
                                $"no contained virtual/abstract method.", frame);

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

            if (Fields->Count != 0) for (var i = 0; i != Fields->Count; i++)
                Fields->Get(i)->init_mapping(frame);

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

        
        public RuntimeIshtarField* FindField(string name)
        {
            var field = Fields->FirstOrNull(x => x->Name.Equals(name));

            if (field is not null)
                return field;

            if (Parent is null)
                return null;

            return Parent->FindField(name);
        }

        public RuntimeIshtarField* FindField(InternedString* name) => FindField(StringStorage.GetStringUnsafe(name));

        public RuntimeIshtarField* FindField(RuntimeFieldName* name)
        {
            var field = Fields->FirstOrNull(x => x->FullName->Equals(name));

            if (field is not null)
                return field;

            if (Parent is null)
                return null;

            return Parent->FindField(name);
        }

        public RuntimeIshtarMethod* GetEntryPoint() =>
            Methods->FirstOrNull(x =>
            {
                if (x->IsSpecial)
                    return false;
                if (!x->IsStatic)
                    return false;
                if (x->IsExtern)
                    return false;
                if (x->ReturnType->TypeCode != TYPE_VOID)
                    return false;
                if (x->ArgLength > 0)
                    return false;
                if (!x->RawName.Equals("master"))
                    return false;
                return true;
            });


        public RuntimeIshtarMethod* GetSpecialEntryPoint(string name) =>
            Methods->FirstOrNull(x =>
            {
                if (!name.EndsWith(")"))
                    throw new InvalidOperationException($"Trying summon method, but methodName is not completed {name}");
                if (!x->IsStatic)
                    return false;
                if (x->ArgLength > 0)
                    return false;
                if (!x->Name.Equals(name))
                    return false;
                return true;
            });

        public RuntimeIshtarMethod* FindMethod(string fullyName, bool searchInParent = true)
        {
            var method = Methods
                ->FirstOrNull(x => x->RawName.Equals(fullyName) || x->Name.Equals(fullyName));

            if (method is not null)
                return method;

            if (Parent is null)
                return null;

            if (!searchInParent)
                return null;

            return Parent->FindMethod(fullyName);
        }

        public RuntimeIshtarMethod* FindMethod(string fullyName, UnsafeFilter_Delegate<RuntimeIshtarMethod> predicate)
        {
            var method = Methods
                ->FirstOrNull(x => (x->RawName.Equals(fullyName) || x->Name.Equals(fullyName)) || predicate(x));

            if (method is not null)
                return method;

            if (Parent is null)
                return null;

            return Parent->FindMethod(fullyName, predicate);
        }

        public RuntimeIshtarMethod* GetDefaultDtor() => _get_tor(VeinMethod.METHOD_NAME_DECONSTRUCTOR);

        public RuntimeIshtarMethod* GetDefaultCtor() => _get_tor(VeinMethod.METHOD_NAME_CONSTRUCTOR);


        private RuntimeIshtarMethod* _get_tor(string name, bool isStatic = false)
            => Methods->FirstOrNull(x => x->IsStatic == isStatic && x->RawName.Equals(name) && (x->IsDeconstructor || x->IsConstructor));

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

        internal void replace_owner(RuntimeIshtarModule* module) => Owner = module;

        public static bool Eq(RuntimeIshtarClass* p1, RuntimeIshtarClass* p2) => RuntimeQualityTypeName.Eq(p1->FullName, p2->FullName);
    }
}
