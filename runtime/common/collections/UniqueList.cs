namespace mana.collections
{
    using System;
    using System.Collections.Generic;

    public class UniqueList<T> : List<T> // TODO
    {
        public new void Add(T t)
        {
            if (Contains(t))
                throw new DuplicateItemException($"'{t}' already added.");
            base.Add(t);
        }

        public new void AddRange(IEnumerable<T> collection)
        {
            foreach (var t in collection) this.Add(t);
        }
    }

    public class DuplicateItemException : Exception
    {
        public DuplicateItemException(string msg) : base(msg)
        {

        }
    }
}
