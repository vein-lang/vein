namespace ishtar;


public static class Marshals
{
    private static Dictionary<Type, ITransitAllocator> _list = new();

    public static TransitAllocator<T> GetFor<T>(CallFrame frame) where T : class
    {
        var key = typeof(T);

        if (_list.ContainsKey(key))
            return _list[typeof(T)] as TransitAllocator<T>;

        frame.ThrowException(KnowTypes.TypeNotFoundFault(frame), "failed fetch transit allocator");

        return null;
    }


    public static void Setup()
    {
        _list.Add(typeof(FileInfo), new FileInfoAllocator());
    }
}
