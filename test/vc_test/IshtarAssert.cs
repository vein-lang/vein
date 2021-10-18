namespace wc_test
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using NUnit.Framework;
    using vein.extensions;

    public static class IshtarAssert
    {
        public static T IsType<T>(object t)
        {
            if (t is T t_0)
                return t_0;
            Assert.IsInstanceOf<T>(t);
            return default;
        }

        public static void SequenceEqual<T, D>(IEnumerable<T> expected, IEnumerable<D> actual) =>
            Assert.AreEqual($"{expected.Select(x => $"{x}").Join(", ")}",
                $"{actual.Select(x => $"{x}").Join(", ")}");

        public static void NotEmpty<T>(IEnumerable<T> t) => Assert.IsNotEmpty(t);

        public static void Single<T>(IEnumerable<T> t)
        {
            if (t?.Count() == 1) { }
            else
                Assert.Fail($"Collection is not contains single element.");
        }

        /// <summary>Verifies that a collection contains a given object.</summary>
        /// <typeparam name="T">The type of the object to be verified</typeparam>
        /// <param name="collection">The collection to be inspected</param>
        /// <param name="filter">The filter used to find the item you're ensuring the collection contains</param>
        /// <exception cref="T:Xunit.Sdk.ContainsException">Thrown when the object is not present in the collection</exception>
        public static void Contains<T>(IEnumerable<T> collection, Predicate<T> filter)
        {
            if (collection == null) throw new ArgumentNullException(nameof(collection));
            if (filter == null) throw new ArgumentNullException(nameof(filter));
            foreach (T obj in collection)
            {
                if (filter(obj))
                    return;
            }
            throw new Xunit.Sdk.ContainsException((object)"(filter expression)", (object)collection);
        }
    }
}
