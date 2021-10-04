namespace vein
{
    using System.Collections.Generic;
    using System.Linq;

    public static class CollectionExtensions
    {
        public static bool AddRange<T>(this HashSet<T> set, IEnumerable<T> value)
            => value.Select(set.Add).All(x => x);
    }
}
