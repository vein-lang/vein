namespace ishtar.collections;

public unsafe interface IWithUnsafeIndex<T> where T : unmanaged
{
    int Size { get; set; }
    T* ElementAt(int index);
}