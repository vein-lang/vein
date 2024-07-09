namespace ishtar
{
    using System.Collections.Generic;

    public static class ReverseControlExtensions
    {
        public static void AddInto<TValue, TKey>(this TValue t, IDictionary<TKey, TValue> store, Func<TValue, TKey> selector)
            => store[selector(t)] = t;
    }
}
