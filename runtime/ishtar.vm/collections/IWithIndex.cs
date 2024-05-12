namespace ishtar.collections;

public interface IWithIndex<T> where T : unmanaged
{
    int Size { get; set; }
    ref T ElementAt(int index);
}