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
}
