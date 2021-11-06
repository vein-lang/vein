namespace vein
{
    using System.Collections.Generic;
    using System.Linq;

    public static class CollectionExtensions
    {
        public static bool AddRange<T>(this HashSet<T> set, IEnumerable<T> value)
            => value.Select(set.Add).All(x => x);

        public static IReadOnlyCollection<TaggetElement<T>> Tagget<T>(this IEnumerable<T> collection)
            => collection
                .Select((x, y) => new TaggetElement<T>(x, (uint)y, collection))
                .ToList()
                .AsReadOnly();
    }

    public class TaggetElement<T>
    {
        public IEnumerable<T> Collection { get; }
        public T Value { get; }
        public uint Index { get; }
        public bool IsLast => Index == Collection.Count() - 1;
        public bool IsFirst => Index == 0;

        public TaggetElement(T value, uint index, IEnumerable<T> collection)
        {
            this.Value = value;
            this.Index = index;
            this.Collection = collection;
        }

        public void Deconstruct(out T value, out uint index, out (bool isLast, bool isFirst) tag)
        {
            value = Value;
            index = Index;
            tag = (this.IsLast, this.IsFirst);
        }
    }
}
