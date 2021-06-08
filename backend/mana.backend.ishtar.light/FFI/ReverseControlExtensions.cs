namespace ishtar
{
    using System;
    using System.Collections.Generic;

    public static class ReverseControlExtensions
    {
        public static void AddInto<TValue, TKey>(this TValue t, IDictionary<TKey, TValue> store, Func<TValue, TKey> selector)
            => store.Add(selector(t), t);
    }
}
