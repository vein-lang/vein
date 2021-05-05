namespace ishtar
{
    using System.Runtime.InteropServices;

    public static unsafe class IshtarUnsafe
    {
        public static void* AsPointer<T>(ref T t) where T : class
        {
            var p = GCHandle.Alloc(t, GCHandleType.WeakTrackResurrection);
            return (void*)GCHandle.ToIntPtr(p);
        }

        public static T AsRef<T>(void* raw) where T : class
        {
            var p = GCHandle.FromIntPtr((nint) raw);
            return p.Target as T;
        }
    }
}