namespace ishtar
{
    using wave.runtime;

    public unsafe class RuntimeIshtarField : WaveField
    {
        public RuntimeIshtarField(WaveClass owner, FieldName fullName, FieldFlags flags, WaveClass fieldType) : 
            base(owner, fullName, flags, fieldType)
        { }


        public int vtable_offset = 0;
        public void* default_value;
    }
}