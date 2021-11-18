namespace vein.extensions
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;

    public static class IEnumerableExtensions
    {
        public static ulong Sum<T>(this IEnumerable<T> enumerable, Func<T, ulong> selector)
            => enumerable.Aggregate<T, ulong>(0, (current, v) => current + selector(v));

        public static string Join(this IEnumerable<char> enumerable)
            => string.Join("", enumerable);
        public static string Join(this IEnumerable<char> enumerable, string key)
            => string.Join(key, enumerable);
        public static string Join(this IEnumerable<char> enumerable, char key)
            => string.Join(key, enumerable);

        public static string Join(this IEnumerable<string> enumerable)
            => string.Join("", enumerable);
        public static string Join(this IEnumerable<string> enumerable, string key)
            => string.Join(key, enumerable);
        public static string Join(this IEnumerable<string> enumerable, char key)
            => string.Join(key, enumerable);

        public static string Join<T>(this IEnumerable<T> enumerable)
            where T : struct, IComparable, IFormattable, IConvertible
            => string.Join(string.Empty, enumerable);
        public static string Join<T>(this IEnumerable<T> enumerable, string key)
            where T : struct, IComparable, IFormattable, IConvertible
            => string.Join(key, enumerable);
        public static string Join<T>(this IEnumerable<T> enumerable, char key)
            where T : struct, IComparable, IFormattable, IConvertible
            => string.Join(key, enumerable);

        public static bool IsNullOrEmpty<T>(this IEnumerable<T> enumerable)
            => enumerable == null || !enumerable.Any();

        public static IEnumerable<T> EmptyIfNull<T>(this IEnumerable<T> enumerable)
            => enumerable ?? Enumerable.Empty<T>();

        public static IEnumerable<T> TrimNull<T>(this IEnumerable<T> enumerable)
            => enumerable.Where(x => x is not null);

        public static IEnumerable<T> OfExactType<T>(this IEnumerable enumerable)
            => enumerable.OfType<T>().Where(t => t.GetType() == typeof(T));

        public static IEnumerable<T> Count<T>(this IEnumerable<T> enumerable, out uint count)
        {
            var collection = enumerable.ToArray();
            count = (uint)collection.Length;
            return collection;
        }
    }
}
