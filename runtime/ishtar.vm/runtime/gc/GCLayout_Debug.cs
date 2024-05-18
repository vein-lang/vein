namespace ishtar.runtime.gc;

public unsafe interface GCLayout_Debug
{
    public void find_leak();
    public void dump(string file);
}
