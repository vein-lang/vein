namespace ishtar
{
    using mana.runtime;

    public unsafe class RuntimeIshtarField : ManaField
    {
        public RuntimeIshtarField(ManaClass owner, FieldName fullName, FieldFlags flags, ManaClass fieldType) : 
            base(owner, fullName, flags, fieldType)
        { }


        public int vtable_offset = 0;
        public void* default_value;
    }
}