namespace ishtar
{
    using collections;
    using runtime;

    public unsafe struct MetaMethodHeader
    {
        public uint code_size { get; set; }
        public uint* code { get; set; }
        public short max_stack { get; set; }
        public DirectNativeList<ProtectedZone>* exception_handler_list { get; set; }
        public UnsafeDictionary<int, ILLabel>* labels_map { get; set; }
        public NativeList<int> labels { get; set; }
    }
}
