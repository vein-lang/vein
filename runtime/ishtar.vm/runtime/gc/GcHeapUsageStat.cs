namespace ishtar.runtime.gc;

public struct GcHeapUsageStat
{
    public long pheap_size;
    public long pfree_bytes;
    public long punmapped_bytes;
    public long pbytes_since_gc;
    public long ptotal_bytes;
}
