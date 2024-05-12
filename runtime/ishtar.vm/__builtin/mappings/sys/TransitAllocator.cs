namespace ishtar;

using vein.runtime;

public unsafe abstract class TransitAllocator<T> : ITransitAllocator where T : class
{
    public abstract IshtarObject* Marshal(T t, CallFrame frame);

    public abstract VeinClass Type { get; }

    public RuntimeIshtarClass* RuntimeType(CallFrame frame)
        => KnowTypes.FromCache(Type.FullName, frame);
}
