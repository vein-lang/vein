namespace ishtar
{
    using System;
    using mana.runtime;

    public struct stackval
    {
        public stack_union data;
        public ManaTypeCode type;

        public static stackval[] Alloc(int size) => GC.AllocateArray<stackval>(size, true);
    }
}