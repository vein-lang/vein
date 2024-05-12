namespace ishtar.collections;

public unsafe delegate bool UnsafePredicate<T>(T* value) where T : unmanaged;