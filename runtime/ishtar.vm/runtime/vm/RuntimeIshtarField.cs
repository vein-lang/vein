namespace ishtar
{
    using System.Linq;
    using collections;
    using vein.runtime;
    using runtime;

    public readonly unsafe struct WeakRef<T>(void* ptr) where T : class
    {
        public T Value => IshtarUnsafe.AsRef<T>(ptr);

        public static WeakRef<T>* Create(T value)
        {
            var r = IshtarGC.AllocateImmortal<WeakRef<T>>();
            *r = new WeakRef<T>(IshtarUnsafe.AsPointer(ref value));
            return r;
        }
    }
    
    public unsafe struct RuntimeIshtarField(
        RuntimeIshtarClass* owner,
        RuntimeFieldName* fullName,
        FieldFlags flags,
        RuntimeIshtarClass* fieldType,
        RuntimeIshtarField* selfRef)
    {
        public RuntimeIshtarClass* Owner { get; } = owner;
        public RuntimeIshtarClass* FieldType { get; private set; } = fieldType;
        public string Name => FullName->Name;
        public ulong vtable_offset;

        public FieldFlags Flags { get; set; } = flags;

        public RuntimeFieldName* FullName { get; set; } = fullName;

        public NativeList<RuntimeAspect>* Aspects { get; } = IshtarGC.AllocateList<RuntimeAspect>();


        public IshtarObject* default_value;

        internal void ReplaceType(RuntimeIshtarClass* type)
        {
            VirtualMachine.Assert(type is not null, WNE.TYPE_LOAD, "[field] Replacing type is nullptr");
            FieldType = type;
        }


        public bool init_mapping(CallFrame frame)
        {
            bool failMapping(int code, RuntimeIshtarField* field)
            {
                frame.vm.FastFail(WNE.TYPE_LOAD,
                    $"Native aspect has incorrect mapping for '{field->FullName->Name}' field. [0x{code:X}]", frame);
                return false;
            }

            var nativeAspect = Aspects->FirstOrNull(x => x->IsNative());
            if (nativeAspect is null)
                return false;
            if (nativeAspect->Arguments->Length != 1)
                return failMapping(0, selfRef);
            var arg = nativeAspect->Arguments->Get(0);

            if (arg->Value->clazz->TypeCode is not VeinTypeCode.TYPE_STRING)
                return failMapping(1, selfRef);

            var existName = IshtarMarshal.ToDotnetString(arg->Value, frame);

            var existField = Owner->FindField(existName);

            if (existField is null)
                return failMapping(2, selfRef);

            if (!FieldType->FullName->Equals(existField->FieldType->FullName))
                return failMapping(3, selfRef);

            vtable_offset = existField->vtable_offset;

            return true;
        }

        public override string ToString() => $"Field '{FullName->Name}': '{FieldType->FullName->NameWithNS}'";
    }
}
