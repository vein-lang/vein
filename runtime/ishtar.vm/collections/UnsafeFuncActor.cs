namespace ishtar.collections;

public unsafe delegate E UnsafeFuncActor<T, out E>(T* value) where T : unmanaged;