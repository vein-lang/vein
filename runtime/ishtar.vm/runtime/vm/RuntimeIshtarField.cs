namespace ishtar
{
    using System.Linq;
    using collections;
    using vein.runtime;
    using runtime;
    using runtime.gc;

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
        RuntimeIshtarField* selfRef) : IEq<RuntimeIshtarField>, IDisposable
    {
        public RuntimeIshtarClass* Owner { get; } = owner;
        public RuntimeIshtarClass* FieldType { get; private set; } = fieldType;
        public string Name => FullName->Name;
        public ulong vtable_offset;

        public FieldFlags Flags { get; set; } = flags;

        public RuntimeFieldName* FullName { get; set; } = fullName;

        public NativeList<RuntimeAspect>* Aspects { get; } = IshtarGC.AllocateList<RuntimeAspect>();


        public void Dispose()
        {
            VirtualMachine.GlobalPrintln($"Disposed field '{Name}'");

            fullName = null;
            owner = null;
            fieldType = null;
            default_value = null;

            Aspects->ForEach(x => x->Dispose());

            IshtarGC.FreeList(Aspects);
            IshtarGC.FreeImmortal(selfRef);
        }


        public bool IsLiteral => Flags.HasFlag(FieldFlags.Literal);
        public bool IsValueType => Owner->IsValueType;

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

            //if (arg->Value == (IshtarObject*)0x14) // marked by internal system
            //{
            //    var field = Owner->FindField(arg->Owner->Union.FieldAspect.FieldName);

            //    var lst = Owner->Fields->ToList();

            //    if (field is null)
            //        return failMapping(2, selfRef);

            //    vtable_offset = field->vtable_offset;
            //    return true;
            //}

            if (arg->Value.type is not VeinTypeCode.TYPE_STRING)
                return failMapping(1, selfRef);

            var existName = (InternedString*)arg->Value.data.p;

            var existField = Owner->FindField(existName);

            if (existField is null)
                return failMapping(2, selfRef);

            
            if (!RuntimeIshtarClass.Eq(FieldType, existField->FieldType))
                return failMapping(3, selfRef);

            vtable_offset = existField->vtable_offset;

            return true;
        }

        public static bool Eq(RuntimeIshtarField* p1, RuntimeIshtarField* p2) => p1->Name.Equals(p2->Name) && p1->Flags == p2->Flags && RuntimeIshtarClass.Eq(p1->FieldType, p2->FieldType);

        public override string ToString() => $"Field '{FullName->Name}': '{FieldType->FullName->NameWithNS}'";
    }
}
