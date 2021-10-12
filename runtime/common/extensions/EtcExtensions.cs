namespace vein.extensions
{
    using System;

    public static class EtcExtensions
    {
        public static void Transition<T1, T2>(this (T1 t1, T2 t2) tuple, Action<T1> ft1, Action<T2> ft2)
        {
            ft1(tuple.t1);
            ft2(tuple.t2);
        }
        public static void Transition<T1, T2, T3>(this (T1 t1, T2 t2, T3 t3) tuple, Action<T1> ft1, Action<T2> ft2, Action<T3> ft3)
        {
            ft1(tuple.t1);
            ft2(tuple.t2);
            ft3(tuple.t3);
        }
    }
}
