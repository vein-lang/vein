namespace ishtar;

public unsafe struct Pointer1D<T>
{
    public Pointer1D(void* p) => _ref = p;
    public void* _ref;
}
