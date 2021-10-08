namespace ishtar
{
    using System.Linq;
    using vein.runtime;

    public unsafe class RuntimeIshtarField : VeinField
    {
        public RuntimeIshtarField(VeinClass owner, FieldName fullName, FieldFlags flags, VeinClass fieldType) :
            base(owner, fullName, flags, fieldType)
        { }

        public uint vtable_offset;
        public void* default_value;


        public bool init_mapping()
        {
            bool failMapping(int code)
            {
                VM.FastFail(WNE.TYPE_LOAD,
                    $"Native aspect has incorrect mapping for '{FullName}' field. [0x{code:X}]");
                VM.ValidateLastError();
                return false;
            }

            var nativeAspect = Aspects.FirstOrDefault(x => x.Name == "Native");
            if (nativeAspect is null)
                return false;
            if (nativeAspect.Arguments.Count != 1)
                return failMapping(0);
            var arg = nativeAspect.Arguments.First().Value;

            if (arg is not string existName)
                return failMapping(1);

            var existField = Owner.FindField(existName);

            if (existField is null)
                return failMapping(2);

            if (existField.FieldType != FieldType)
                return failMapping(3);

            if (existField is not RuntimeIshtarField runtimeField)
                return failMapping(4);

            vtable_offset = runtimeField.vtable_offset;

            return true;
        }


        public override string ToString() => $"Field '{FullName}': '{FieldType.FullName.NameWithNS}'";
    }
}
