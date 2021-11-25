namespace ishtar
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Runtime.InteropServices;

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
            if (raw == null) throw new ArgumentNullException(nameof(raw));
            var p = GCHandle.FromIntPtr((nint) raw);
            var r = p.Target as T;
            GC.ReRegisterForFinalize(r ?? throw new InvalidOperationException());
            return r;
        }
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
