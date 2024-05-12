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
    
    public unsafe struct RuntimeIshtarField
    {
        private readonly WeakRef<VeinField>* _field;
        public RuntimeIshtarClass* Owner { get; }
        public RuntimeIshtarClass* FieldType { get; }
        public string Name => _field->Value.Name;
        public FieldFlags Flags => _field->Value.Flags;
        public ulong vtable_offset;

        public FieldName FullName => _field->Value.FullName;

        public DirectNativeList<RuntimeAspect>* Aspects { get; } = DirectNativeList<RuntimeAspect>.New(4);


        public RuntimeIshtarField(RuntimeIshtarClass* owner, RuntimeFieldName* fullName, FieldFlags flags, RuntimeIshtarClass* fieldType)
        {
            var @base = new VeinField(owner->Original, new FieldName(fullName->Name, fullName->Class), flags, fieldType->Original);
            Owner = owner;
            FieldType = fieldType;
            _field = WeakRef<VeinField>.Create(@base);
        }

        public IshtarObject* default_value;

        internal void ReplaceType(RuntimeIshtarClass* type)
        {

        }


        public bool init_mapping(CallFrame frame)
        {
            bool failMapping(int code, WeakRef<VeinField>* val)
            {
                frame.vm.FastFail(WNE.TYPE_LOAD,
                    $"Native aspect has incorrect mapping for '{val->Value.FullName}' field. [0x{code:X}]", frame);
                return false;
            }

            var nativeAspect = _field->Value.Aspects.FirstOrDefault(x => x.Name == "Native");
            if (nativeAspect is null)
                return false;
            if (nativeAspect.Arguments.Count != 1)
                return failMapping(0, _field);
            var arg = nativeAspect.Arguments.First().Value;

            if (arg is not string existName)
                return failMapping(1, _field);

            var existField = Owner->FindField(existName);

            if (existField is null)
                return failMapping(2, _field);

            if (existField->FieldType->Original.FullName != _field->Value.FieldType.FullName)
                return failMapping(3, _field);

            vtable_offset = existField->vtable_offset;

            return true;
        }

        public override string ToString() => $"Field '{FullName}': '{FieldType->FullName->NameWithNS}'";
    }
}
