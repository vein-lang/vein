namespace ishtar.collections;

public unsafe interface IUnsafeNativeList<T> : IWithUnsafeIndex<T> where T : unmanaged
{
    int Capacity { get; set; }
    bool IsEmpty { get; }
    T* this[int index] { get; set; }
    void Clear();
}