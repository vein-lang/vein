namespace ishtar.collections;

public interface INativeList<T> : IWithIndex<T> where T : unmanaged
{
    int Capacity { get; set; }
    bool IsEmpty { get; }
    T this[int index] { get; set; }
    void Clear();
}