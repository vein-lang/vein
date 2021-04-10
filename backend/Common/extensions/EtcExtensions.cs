namespace wave.extensions
{
    using System;

    public static class EtcExtensions
    {
        public static void Transition<T1, T2>(this (T1 t1, T2 t2) tuple, Action<T1> ft1, Action<T2> ft2)
        {
            ft1(tuple.t1);
            ft2(tuple.t2);
        }
    }
}