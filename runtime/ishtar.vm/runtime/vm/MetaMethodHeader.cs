namespace ishtar;

using collections;
using runtime;

[CTypeExport("ishtar_method_header_t")]
public unsafe struct MetaMethodHeader
{
    public uint code_size { get; set; }
    public uint* code { get; set; }
    public short max_stack { get; set; }
    public NativeList<ProtectedZone>* exception_handler_list { get; set; }
    public AtomicNativeDictionary<int, ILLabel>* labels_map { get; set; }
    public AtomicNativeList<int>* labels { get; set; }
}
