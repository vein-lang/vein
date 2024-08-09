namespace ishtar
{
    using System.Diagnostics.CodeAnalysis;

    public static unsafe class IshtarUnsafe
    {
        public static void* AsPointer<T>([NotNull] ref T t) where T : class
        {
            if (t == null) throw new ArgumentNullException(nameof(t));
            GC.SuppressFinalize(t);
            var p = GCHandle.Alloc(t, GCHandleType.WeakTrackResurrection);
            return (void*)GCHandle.ToIntPtr(p);
        }

        public static T AsRef<T>(void* raw) where T : class
        {
#if DEBUG
            if (raw == null) return null;
#endif
            if (raw == null) throw new ArgumentNullException(nameof(raw));
            var p = GCHandle.FromIntPtr((nint) raw);
            var r = p.Target as T;

            if (r is null) return r;
            GC.ReRegisterForFinalize(r);
            return r;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct aligned_data<T> where T : unmanaged
        {
            public byte dummy;
            public T data;
        }

        public static void CopyBlock(void* dest, void* source, uint bytes) => Unsafe.CopyBlock(dest, source, bytes);

        public static unsafe void MemSet(void* dst, int value, ulong numberOfBytes)
        {
            // Copy per 8 bytes
            {
                ulong v = ((uint)value) | ((uint)value << 32);
                ulong* dst8 = (ulong*)dst;
                var count = numberOfBytes >> 3; // divide by 8
                ulong i = 0;
                for (; i < count; ++i)
                    dst8[i] = v;

                // Get remainder
                dst = (void*)dst8;
                numberOfBytes -= count;
            }

            // Copy per byte
            {
                byte* v = stackalloc byte[4];
                v[0] = (byte)(((uint)value) & 0xF);
                v[1] = (byte)((((uint)value) >> 4) & 0xF);
                v[2] = (byte)((((uint)value) >> 8) & 0xF);
                v[3] = (byte)((((uint)value) >> 12) & 0xF);
                byte* dst1 = (byte*)dst;
                var count = numberOfBytes; // remainder
                ulong i = 0;
                for (; i < count; ++i)
                    dst1[i] = v[i % 4];
            }
        }

        public static int AlignOf<T>() where T : unmanaged
            => sizeof(aligned_data<T>) - sizeof(T);

        public static void* MemoryCopy<T>(T* newPointer, T* @ref, int bytesToCopy) where T : unmanaged
            => memcpy(newPointer, @ref, (UIntPtr)bytesToCopy);

        public static void* MemoryCopy(void* newPointer, void* @ref, int bytesToCopy)
            => memcpy(newPointer, @ref, (UIntPtr)bytesToCopy);

        // TODO linux
        [DllImport("msvcrt.dll", EntryPoint = "memcpy", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        public static extern void* memcpy(void* dest, void* src, UIntPtr count);


        public static void MoveMemory(void* dest, void* source, int length)
            => MoveMemory((IntPtr)dest, (IntPtr)source, (uint)length);

        [DllImport("kernel32.dll", CallingConvention = CallingConvention.StdCall)]
        public static extern void MoveMemory(IntPtr dest,
            IntPtr source,
            uint length);

        // TODO
        public static void WriteArrayElement<TValue>(byte* ptr, int idx, TValue item) where TValue : unmanaged
            => Unsafe.Write(ptr + (idx * sizeof(TValue)), item);
        // TODO
        public static TValue ReadArrayElement<TValue>(byte* b, int idx) where TValue : unmanaged
            => Unsafe.Read<TValue>(b + (idx * sizeof(TValue)));
    }

    

    public ref struct ImmortalObject<T> where T : class
    {
        public IntPtr Pointer { get; private set; }
        public void Create([NotNull] T t)
        {
            if (t == null) throw new ArgumentNullException(nameof(t));

            GC.SuppressFinalize(t);
            var p = GCHandle.Alloc(t, GCHandleType.WeakTrackResurrection);
            this.Pointer = GCHandle.ToIntPtr(p);
        }

        public T Value => GCHandle.FromIntPtr((nint)Pointer).Target as T;

        public void Delete()
        {
            if (Pointer == default) throw new ArgumentNullException(nameof(Pointer));
            var p = GCHandle.FromIntPtr((nint) Pointer);
            var r = p.Target as T;
            GC.ReRegisterForFinalize(r ?? throw new InvalidOperationException());
        }
    }
}
