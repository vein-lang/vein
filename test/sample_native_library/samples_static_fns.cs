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
        public delegate int printf_str(string format, string arg0);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi, SetLastError = false)]
        public delegate int printf_int(string format, int arg0);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi, SetLastError = false)]
        public delegate int printf_long(string format, long arg0);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi, SetLastError = false)]
        public delegate int printf_float(string format, float arg0);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi, SetLastError = false)]
        public delegate int printf_double(string format, double arg0);


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

        [UnmanagedCallersOnly(EntryPoint = "_sample_2_1")]
        public static void _sample_2_1(int i1, int i2, int i3, int i4)
            => Console.WriteLine($"{nameof(_sample_2_1)} has been called! args: i1 {i1}, i2 {i2}, i3: {i3}, i4: {i4}");

        [UnmanagedCallersOnly(EntryPoint = "_sample_2_2")]
        public static void _sample_2_2(int i1, int i2, int i3, int i4, int i5)
            => Console.WriteLine($"{nameof(_sample_2_2)} has been called! args: i1 {i1}, i2 {i2}, i3: {i3}, i4: {i4}, i5: {i5}");

        [UnmanagedCallersOnly(EntryPoint = "_sample_2_3")]
        public static int _sample_2_3(int i1, int i2, int i3, int i4, int i5)
        {
            Console.WriteLine(
                $"{nameof(_sample_2_3)} has been called! args: i1 {i1}, i2 {i2}, i3: {i3}, i4: {i4}, i5: {i5}");
            return i1 + i2 + i3 + i4;
        }

        [UnmanagedCallersOnly(EntryPoint = "_sample_2_4")]
        public static long _sample_2_4(long i1, long i2, long i3, long i4, long i5)
        {
            Console.WriteLine(
                $"{nameof(_sample_2_4)} has been called! args: i1 {i1}, i2 {i2}, i3: {i3}, i4: {i4}, i5: {i5}");
            return i1 + i2 + i3 + i4;
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

        [UnmanagedCallersOnly(EntryPoint = "_sample_6")]
        public unsafe static AB _sample_6(AB i)
        {
            Console.WriteLine($"i: {i.s1} {i.s2}");
            i.s2 = 150;
            return i;
        }



        [UnmanagedCallersOnly(EntryPoint = "_sample_7")]
        public static long _sample_7(long i)
        {
            var lib = NativeLibrary.Load("msvcrt.dll");
            var ptr = NativeLibrary.GetExport(lib, "printf");
            var func = Marshal.GetDelegateForFunctionPointer<printf_long>(ptr);
            var len = func.Invoke($"{nameof(_sample_7)} has been called! args: i: %d\n", i);
            return i;
        }

        [UnmanagedCallersOnly(EntryPoint = "_sample_7_1")]
        public static long _sample_7_1(long i1, long i2)
        {
            var lib = NativeLibrary.Load("msvcrt.dll");
            var ptr = NativeLibrary.GetExport(lib, "printf");
            var func = Marshal.GetDelegateForFunctionPointer<printf_long>(ptr);
            var len = func.Invoke($"{nameof(_sample_7_1)} has been called! args: i: %d\n", i1 + i2);
            return i1 + i2;
        }

        [UnmanagedCallersOnly(EntryPoint = "_sample_7_2")]
        public static long _sample_7_2(long i1, long i2, long i3, long i4)
        {
            var lib = NativeLibrary.Load("msvcrt.dll");
            var ptr = NativeLibrary.GetExport(lib, "printf");
            var func = Marshal.GetDelegateForFunctionPointer<printf_long>(ptr);
            var len = func.Invoke($"{nameof(_sample_7_2)} has been called! args: i: %d\n", i1 + i2 + i3 + i4);
            return i1 + i2 + i3 + i4;
        }

        [UnmanagedCallersOnly(EntryPoint = "_sample_8")]
        public static float _sample_8(float i1, float i2, float i3, float i4)
        {
            var lib = NativeLibrary.Load("msvcrt.dll");
            var ptr = NativeLibrary.GetExport(lib, "printf");
            var func = Marshal.GetDelegateForFunctionPointer<printf_str>(ptr);
            var len = func.Invoke($"{nameof(_sample_8)} has been called! args: i: %d\n, {i1}, {i2}, {i3}, {i4}", $"{i1 + i2 + i3 + i4}");
            return i1 + i2 + i3 + i4;
        }

        [UnmanagedCallersOnly(EntryPoint = "_sample_8_1")]
        public static float _sample_8_1() => 14.48f;


        [UnmanagedCallersOnly(EntryPoint = "_sample_9")]
        public static double _sample_7_2(double i1, double i2, double i3, double i4)
        {
            var lib = NativeLibrary.Load("msvcrt.dll");
            var ptr = NativeLibrary.GetExport(lib, "printf");
            var func = Marshal.GetDelegateForFunctionPointer<printf_double>(ptr);
            var len = func.Invoke($"{nameof(_sample_7_2)} has been called! args: i: %d\n", i1 + i2 + i3 + i4);
            return i1 + i2 + i3 + i4;
        }

        [UnmanagedCallersOnly(EntryPoint = "_sample_7_direct")]
        public static unsafe void DirectCall(long i1) => ((long*)(0xB0BC))[0] = ((delegate*<long, long>)(void*)(0xDEAD))(666_666_666_666);

        // ═══════════════════════════════════════════════════════════════════
        // Struct-related exports for trampoline tests
        // ═══════════════════════════════════════════════════════════════════

        /// <summary>Accepts a struct pointer, returns sum of fields.</summary>
        [UnmanagedCallersOnly(EntryPoint = "_struct_sum_fields")]
        public static unsafe int StructSumFields(AB* ptr)
            => ptr->s1 + ptr->s2;

        /// <summary>Modifies struct fields through pointer.</summary>
        [UnmanagedCallersOnly(EntryPoint = "_struct_modify")]
        public static unsafe void StructModify(AB* ptr, int newS1, int newS2)
        {
            ptr->s1 = newS1;
            ptr->s2 = newS2;
        }

        /// <summary>Accepts struct by value (8 bytes, fits in register on Windows).</summary>
        [UnmanagedCallersOnly(EntryPoint = "_struct_byval_sum")]
        public static int StructByValSum(AB val)
            => val.s1 + val.s2;

        /// <summary>Returns a struct pointer (allocated pinned).</summary>
        [UnmanagedCallersOnly(EntryPoint = "_struct_create")]
        public static unsafe AB* StructCreate(int s1, int s2)
        {
            var ptr = (AB*)NativeMemory.AllocZeroed((nuint)sizeof(AB));
            ptr->s1 = s1;
            ptr->s2 = s2;
            return ptr;
        }

        /// <summary>Accepts pointer + int, returns field sum + extra.</summary>
        [UnmanagedCallersOnly(EntryPoint = "_struct_ptr_plus_int")]
        public static unsafe int StructPtrPlusInt(AB* ptr, int extra)
            => ptr->s1 + ptr->s2 + extra;

        // For fucking aot, it required entry point
        static void Main(string[] args)
        {
        }
    }
}
