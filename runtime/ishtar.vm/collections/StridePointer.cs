namespace ishtar.collections;

public readonly unsafe struct StridePointer<T> where T : class
{
    private readonly void* @ref;
    public StridePointer(T t) => @ref = IshtarUnsafe.AsPointer(ref t);
    public T Value => IshtarUnsafe.AsRef<T>(@ref);
}
