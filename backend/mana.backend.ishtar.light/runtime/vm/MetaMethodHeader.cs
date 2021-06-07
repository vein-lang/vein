namespace ishtar
{
    using System.Collections.Generic;

    public unsafe struct MetaMethodHeader
    {
        public uint code_size;
        public uint* code;
        public short max_stack;
        public uint local_var_sig_tok;
        public uint init_locals;
        public void* exception_handler_list;
        public Dictionary<int, ILLabel> labels_map;
        public List<int> labels;
    }
}