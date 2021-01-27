namespace wave.stl
{
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;

    public static class IEnumerableExtensions
    {
        public static bool IsNullOrEmpty<T>(this IEnumerable<T> enumerable) 
            => enumerable == null || !enumerable.Any();

        public static IEnumerable<T> EmptyIfNull<T>(this IEnumerable<T> enumerable) 
            => enumerable ?? Enumerable.Empty<T>();

        public static IEnumerable<T> OfExactType<T>(this IEnumerable enumerable) 
            => enumerable.OfType<T>().Where(t => t.GetType() == typeof(T));
    }
}