namespace sample_native_library
{
    using System.Runtime.InteropServices;

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct AB
    {
        public int s1;
        public int s2;
    }
    public static class FooBar
    {
        [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi, SetLastError = false)]
        public delegate int printf_int(string format, int arg0);


        [UnmanagedCallersOnly(EntryPoint = "_sample_1")]
        public static void _sample_1()
        {
            var lib = NativeLibrary.Load("msvcrt.dll");
            var ptr = NativeLibrary.GetExport(lib, "printf");
            var func = Marshal.GetDelegateForFunctionPointer<printf_int>(ptr);
            var len = func.Invoke($"{nameof(_sample_1)} has been called!", 0);
        }

        [UnmanagedCallersOnly(EntryPoint = "_sample_2")]
        public static void _sample_2(int i)
        {
            var lib = NativeLibrary.Load("msvcrt.dll");
            var ptr = NativeLibrary.GetExport(lib, "printf");
            var func = Marshal.GetDelegateForFunctionPointer<printf_int>(ptr);
            var len = func.Invoke($"{nameof(_sample_2)} has been called! args: i: %d\n", i);
        }

        [UnmanagedCallersOnly(EntryPoint = "_sample_3")]
        public static int _sample_3(int i)
        {
            var lib = NativeLibrary.Load("msvcrt.dll");
            var ptr = NativeLibrary.GetExport(lib, "printf");
            var func = Marshal.GetDelegateForFunctionPointer<printf_int>(ptr);
            var len = func.Invoke($"{nameof(_sample_2)} has been called! args: i: %d\n", i);
            return i;
        }

        [UnmanagedCallersOnly(EntryPoint = "_sample_4")]
        public static void _sample_4(AB i)
        {
            var lib = NativeLibrary.Load("msvcrt.dll");
            var ptr = NativeLibrary.GetExport(lib, "printf");
            var func = Marshal.GetDelegateForFunctionPointer<printf_int>(ptr);
            var len = func.Invoke($"{nameof(_sample_2)} has been called! args: i: %d\n", i.s1 + i.s2);
        }

        [UnmanagedCallersOnly(EntryPoint = "_sample_5")]
        public unsafe static int* _sample_5(int i)
        {
            Console.WriteLine($"i: {i}");
            return &i;
        }

        [UnmanagedCallersOnly(EntryPoint = "_sample_5")]
        public unsafe static AB _sample_6(AB i)
        {
            Console.WriteLine($"i: {i.s1} {i.s2}");
            i.s2 = 150;
            return i;
        }


        // For fucking aot, it required entry point
        static void Main(string[] args)
        {
        }
    }
}
