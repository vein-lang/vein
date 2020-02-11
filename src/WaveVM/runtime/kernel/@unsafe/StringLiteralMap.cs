namespace wave.runtime.kernel.@unsafe
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Security;

    /*
     * |                                      Method |      Mean |     Error |    StdDev | Rank |
     * |-------------------------------------------- |----------:|----------:|----------:|-----:|
     * |        'intern string [managed non-insert]' |  4.604 ns | 0.0782 ns | 0.0732 ns |    1 |
     * |         'intern string [native non-insert]' |  4.838 ns | 0.0551 ns | 0.0515 ns |    2 |
     * |  'intern string [native insert, auto free]' | 55.333 ns | 0.3792 ns | 0.3361 ns |    3 |
     * | 'intern string [managed insert, auto free]' | 64.734 ns | 0.3740 ns | 0.3316 ns |    4 |
     */

    public static unsafe class StringLiteralMap
    {
        [SecurityCritical]
        public static NativeString GetInternedString(int index) => literalStorage.FirstOrDefault(x => x.GetHashCode() == index);

        [SecurityCritical]
        public static void InternString(NativeString str)
        {
            literalStorage.Add(str);
            GC.KeepAlive(str);
        }

        public static void Clear() => literalStorage.Clear();

        public static bool Has(int index) => literalStorage.Any(x => x.GetHashCode() == index);


        #region private

        private static readonly HashSet<NativeString> literalStorage = new HashSet<NativeString>();

        static StringLiteralMap()
        {
            GC.KeepAlive(literalStorage);
        }

        #endregion private
    }
}