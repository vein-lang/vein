namespace ishtar;

public unsafe struct Pointer2D<T>
{
    public Pointer2D(void** p) => _ref = p;
    public void** _ref;
}
