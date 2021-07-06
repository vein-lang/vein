namespace mana.lsp
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Threading;
    using syntax;

    /// <summary>
    /// This class contains information about everything that impacts type checking on a global scope.
    /// </summary>
    internal class FileHeader // *don't* dispose of the sync root!
    {
        public ReaderWriterLockSlim SyncRoot { get; }

        // IMPORTANT: quite a couple of places rely on these being sorted!
        private readonly ManagedSortedSet namespaceDeclarations;
        private readonly ManagedSortedSet openDirectives;
        private readonly ManagedSortedSet typeDeclarations;
        private readonly ManagedSortedSet callableDeclarations;

        public FileHeader(ReaderWriterLockSlim syncRoot)
        {
            this.SyncRoot = syncRoot;
            this.namespaceDeclarations = new ManagedSortedSet(syncRoot);
            this.openDirectives = new ManagedSortedSet(syncRoot);
            this.typeDeclarations = new ManagedSortedSet(syncRoot);
            this.callableDeclarations = new ManagedSortedSet(syncRoot);
        }

        /// <summary>
        /// Invalidates (i.e. removes) all elements in the range [<paramref name="start"/>, <paramref name="start"/> + <paramref name="count"/>), and
        /// updates all elements that are larger than or equal to <paramref name="start"/> + <paramref name="count"/> with <paramref name="lineNrChange"/>.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="start"/> or <paramref name="count"/> are negative, or <paramref name="lineNrChange"/> is smaller than -<paramref name="count"/>.
        /// </exception>
        public void InvalidateOrUpdate(int start, int count, int lineNrChange)
        {
            this.namespaceDeclarations.InvalidateOrUpdate(start, count, lineNrChange);
            this.openDirectives.InvalidateOrUpdate(start, count, lineNrChange);
            this.typeDeclarations.InvalidateOrUpdate(start, count, lineNrChange);
            this.callableDeclarations.InvalidateOrUpdate(start, count, lineNrChange);
        }

        /// <summary>
        /// Returns an array with the *sorted* line numbers containing namespace declarations.
        /// </summary>
        public int[] GetNamespaceDeclarations()
        {
            return this.namespaceDeclarations.ToArray();
        }

        /// <summary>
        /// Returns an array with the *sorted* line numbers containing open directives.
        /// </summary>
        public int[] GetOpenDirectives()
        {
            return this.openDirectives.ToArray();
        }

        /// <summary>
        /// Returns an array with the *sorted* line numbers containing type declarations.
        /// </summary>
        public int[] GetTypeDeclarations()
        {
            return this.typeDeclarations.ToArray();
        }

        /// <summary>
        /// Returns an array with the *sorted* line numbers containing callable declarations.
        /// </summary>
        public int[] GetCallableDeclarations()
        {
            return this.callableDeclarations.ToArray();
        }

        public void AddNamespaceDeclarations(IEnumerable<int> declarations) =>
            this.namespaceDeclarations.Add(declarations);

        public void AddOpenDirectives(IEnumerable<int> declarations) =>
            this.openDirectives.Add(declarations);

        public void AddTypeDeclarations(IEnumerable<int> declarations) =>
            this.typeDeclarations.Add(declarations);

        public void AddCallableDeclarations(IEnumerable<int> declarations) =>
            this.callableDeclarations.Add(declarations);

        public static bool IsNamespaceDeclaration(BaseSyntax fragment) => false;
        //fragment?.Kind != null && fragment.IncludeInCompilation && fragment.Kind.IsNamespaceDeclaration;

        public static bool IsOpenDirective(BaseSyntax fragment) => false;
        //fragment?.Kind != null && fragment.IncludeInCompilation && fragment.Kind.IsOpenDirective;

        public static bool IsTypeDeclaration(BaseSyntax fragment) => false;
        // fragment?.Kind != null && fragment.IncludeInCompilation && fragment.Kind.IsTypeDefinition;

        public static bool IsCallableDeclaration(BaseSyntax fragment) => false;
        // fragment?.Kind != null && fragment.IncludeInCompilation && (
        //     fragment.Kind.IsOperationDeclaration ||
        //    fragment.Kind.IsFunctionDeclaration);

        public static bool IsCallableSpecialization(BaseSyntax fragment) => false;
        //fragment?.Kind != null && fragment.IncludeInCompilation && (
        //        fragment.Kind.IsBodyDeclaration ||
        //        fragment.Kind.IsAdjointDeclaration ||
        //        fragment.Kind.IsControlledDeclaration ||
        //        fragment.Kind.IsControlledAdjointDeclaration);

        public static bool IsHeaderItem(BaseSyntax fragment) =>
            IsNamespaceDeclaration(fragment) ||
            IsOpenDirective(fragment) ||
            IsTypeDeclaration(fragment) ||
            IsCallableDeclaration(fragment) ||
            IsCallableSpecialization(fragment);

        public static IEnumerable<BaseSyntax>? FilterNamespaceDeclarations(IEnumerable<BaseSyntax> fragments) =>
            fragments?.Where(IsNamespaceDeclaration);

        public static IEnumerable<BaseSyntax>? FilterOpenDirectives(IEnumerable<BaseSyntax> fragments) =>
            fragments?.Where(IsOpenDirective);

        public static IEnumerable<BaseSyntax>? FilterTypeDeclarations(IEnumerable<BaseSyntax> fragments) =>
            fragments?.Where(IsTypeDeclaration);

        public static IEnumerable<BaseSyntax>? FilterCallableDeclarations(IEnumerable<BaseSyntax> fragments) =>
            fragments?.Where(IsCallableDeclaration);

        public static IEnumerable<BaseSyntax>? FilterCallableSpecializations(IEnumerable<BaseSyntax> fragments) =>
            fragments?.Where(IsCallableSpecialization);
    }
    /// <summary>
    /// Thread-safe wrapper to <see cref="SortedSet{T}"/> whose generic type argument is <see cref="int"/>.
    /// </summary>
    public class ManagedSortedSet // *don't* dispose of the sync root!
    {
        private SortedSet<int> set = new SortedSet<int>();

        public ReaderWriterLockSlim SyncRoot { get; }

        public ManagedSortedSet(ReaderWriterLockSlim syncRoot)
        {
            this.SyncRoot = syncRoot;
        }

        public int[] ToArray()
        {
            this.SyncRoot.EnterReadLock();
            try
            {
                return this.set.ToArray();
            }
            finally
            {
                this.SyncRoot.ExitReadLock();
            }
        }

        public void Add(IEnumerable<int> items)
        {
            this.SyncRoot.EnterWriteLock();
            try
            {
                this.set.UnionWith(items);
            }
            finally
            {
                this.SyncRoot.ExitWriteLock();
            }
        }

        public void Add(params int[] items)
        {
            this.Add((IEnumerable<int>)items);
        }

        /// <summary>
        /// Clears all elements from the set and returns the removed elements.
        /// </summary>
        public SortedSet<int> Clear()
        {
            this.SyncRoot.EnterWriteLock();
            try
            {
                return this.set;
            }
            finally
            {
                this.set = new SortedSet<int>();
                this.SyncRoot.ExitWriteLock();
            }
        }

        /// <summary>
        /// Removes all elements in the range [<paramref name="start"/>, <paramref name="start"/> + <paramref name="count"/>) from the set, and
        /// updates all elements that are larger than or equal to <paramref name="start"/> + <paramref name="count"/> with lineNr =&gt; lineNr + <paramref name="lineNrChange"/>.
        /// </summary>
        /// <returns>
        /// The number of removed elements.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="start"/> or <paramref name="count"/> are negative, or <paramref name="lineNrChange"/> is smaller than -<paramref name="count"/>.</exception>
        public int InvalidateOrUpdate(int start, int count, int lineNrChange)
        {
            if (start < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(start));
            }

            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            if (lineNrChange < -count)
            {
                throw new ArgumentOutOfRangeException(nameof(lineNrChange));
            }

            this.SyncRoot.EnterWriteLock();
            try
            {
                var nrRemoved = this.set.RemoveWhere(lineNr => start <= lineNr && lineNr < start + count);
                if (lineNrChange != 0)
                {
                    var updatedLineNrs = this.set.Where(lineNr => lineNr >= start + count).Select(lineNr => lineNr + lineNrChange).ToArray(); // calling ToArray to make sure updateLineNrs is not affected by RemoveWhere below
                    this.set.RemoveWhere(lineNr => start <= lineNr);
                    this.set.UnionWith(updatedLineNrs);
                }

                return nrRemoved;
            }
            finally
            {
                this.SyncRoot.ExitWriteLock();
            }
        }
    }
    /// <summary>
    /// Thread-safe wrapper to <see cref="HashSet{T}"/>.
    /// </summary>
    public class ManagedHashSet<T> // *don't* dispose of the sync root!
    {
        private readonly HashSet<T> content = new HashSet<T>();

        public ReaderWriterLockSlim SyncRoot { get; }

        public ManagedHashSet(IEnumerable<T> collection, ReaderWriterLockSlim syncRoot)
        {
            this.content = new HashSet<T>(collection);
            this.SyncRoot = syncRoot;
        }

        public ManagedHashSet(ReaderWriterLockSlim syncRoot)
            : this(Enumerable.Empty<T>(), syncRoot)
        {
        }

        public void Add(T item)
        {
            this.SyncRoot.EnterWriteLock();
            try
            {
                this.content.Add(item);
            }
            finally
            {
                this.SyncRoot.ExitWriteLock();
            }
        }

        public void RemoveAll(Func<T, bool> condition)
        {
            this.SyncRoot.EnterWriteLock();
            try
            {
                this.content.RemoveWhere(item => condition(item));
            }
            finally
            {
                this.SyncRoot.ExitWriteLock();
            }
        }

        public ImmutableHashSet<T> ToImmutableHashSet()
        {
            this.SyncRoot.EnterReadLock();
            try
            {
                return this.content.ToImmutableHashSet();
            }
            finally
            {
                this.SyncRoot.ExitReadLock();
            }
        }
    }

    /// <summary>
    /// Thread-safe wrapper to <see cref="List{T}"/>.
    /// </summary>
    public class ManagedList<T> // *don't* dispose of the sync root!
    {
        private List<T> content = new List<T>();

        public ReaderWriterLockSlim SyncRoot { get; }

        private ManagedList(List<T> content, ReaderWriterLockSlim syncRoot)
        {
            this.content = content;
            this.SyncRoot = syncRoot;
        }

        public ManagedList(IEnumerable<T> collection, ReaderWriterLockSlim syncRoot)
            : this(collection.ToList(), syncRoot)
        {
        }

        public ManagedList(ReaderWriterLockSlim syncRoot)
            : this(new List<T>(), syncRoot)
        {
        }

        /* members for content manipulation */

        public void Add(T item)
        {
            this.SyncRoot.EnterWriteLock();
            try
            {
                this.content.Add(item);
            }
            finally
            {
                this.SyncRoot.ExitWriteLock();
            }
        }

        public void AddRange(IEnumerable<T> elements)
        {
            this.SyncRoot.EnterWriteLock();
            try
            {
                this.content.AddRange(elements);
            }
            finally
            {
                this.SyncRoot.ExitWriteLock();
            }
        }

        public void RemoveRange(int index, int count)
        {
            this.SyncRoot.EnterWriteLock();
            try
            {
                this.content.RemoveRange(index, count);
            }
            finally
            {
                this.SyncRoot.ExitWriteLock();
            }
        }

        public void RemoveAll(Func<T, bool> condition)
        {
            this.SyncRoot.EnterWriteLock();
            try
            {
                this.content.RemoveAll(item => condition(item));
            }
            finally
            {
                this.SyncRoot.ExitWriteLock();
            }
        }

        public void InsertRange(int index, IEnumerable<T> newContent)
        {
            this.SyncRoot.EnterWriteLock();
            try
            {
                this.content.InsertRange(index, newContent);
            }
            finally
            {
                this.SyncRoot.ExitWriteLock();
            }
        }

        public IEnumerable<T> Get()
        {
            this.SyncRoot.EnterReadLock();
            try
            {
                // creates a shallow copy
                return this.content.GetRange(0, this.content.Count);
            }
            finally
            {
                this.SyncRoot.ExitReadLock();
            }
        }

        public IEnumerable<T> GetRange(int index, int count)
        {
            this.SyncRoot.EnterReadLock();
            try
            {
                // creates a shallow copy
                return this.content.GetRange(index, count);
            }
            finally
            {
                this.SyncRoot.ExitReadLock();
            }
        }

        public T GetItem(int index)
        {
            this.SyncRoot.EnterReadLock();
            try
            {
                return this.content[index];
            }
            finally
            {
                this.SyncRoot.ExitReadLock();
            }
        }

        /// <summary>
        /// Gets the item at <paramref name="index"/> if <paramref name="index"/> does not exceed the bounds of the list.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is less than 0.</exception>
        internal bool TryGetItem(int index, [MaybeNullWhen(false)] out T item)
        {
            this.SyncRoot.EnterReadLock();
            try
            {
                if (index < this.content.Count)
                {
                    item = this.content[index];
                    return true;
                }

                item = default;
                return false;
            }
            finally
            {
                this.SyncRoot.ExitReadLock();
            }
        }

        public void SetItem(int index, T newItem)
        {
            this.SyncRoot.EnterWriteLock();
            try
            {
                this.content[index] = newItem;
            }
            finally
            {
                this.SyncRoot.ExitWriteLock();
            }
        }

        public void Replace(int start, int count, IReadOnlyList<T> replacements)
        {
            this.SyncRoot.EnterWriteLock();
            try
            {
                if (replacements.Count - count != 0)
                {
                    this.content.RemoveRange(start, count);
                    this.content.InsertRange(start, replacements);
                }
                else
                {
                    for (var offset = 0; offset < count; ++offset)
                    {
                        this.content[start + offset] = replacements[offset];
                    }
                }
            }
            finally
            {
                this.SyncRoot.ExitWriteLock();
            }
        }

        public void ReplaceAll(IEnumerable<T> replacement)
        {
            this.SyncRoot.EnterWriteLock();
            try
            {
                this.content = replacement.ToList();
            }
            finally
            {
                this.SyncRoot.ExitWriteLock();
            }
        }

        public void ReplaceAll(ManagedList<T> replacement)
        {
            this.SyncRoot.EnterWriteLock();
            try
            {
                this.content = replacement.ToList();
            }
            finally
            {
                this.SyncRoot.ExitWriteLock();
            }
        }

        public void Transform(int index, Func<T, T> transformation)
        {
            this.SyncRoot.EnterWriteLock();
            try
            {
                this.content[index] = transformation(this.content[index]);
            }
            finally
            {
                this.SyncRoot.ExitWriteLock();
            }
        }

        public void Transform(Func<T, T> transformation)
        {
            this.SyncRoot.EnterWriteLock();
            try
            {
                for (var index = 0; index < this.content.Count(); ++index)
                {
                    this.content[index] = transformation(this.content[index]);
                }
            }
            finally
            {
                this.SyncRoot.ExitWriteLock();
            }
        }

        public List<T> Clear()
        {
            this.SyncRoot.EnterWriteLock();
            try
            {
                return this.content;
            }
            finally
            {
                this.content = new List<T>();
                this.SyncRoot.ExitWriteLock();
            }
        }

        public int Count()
        {
            this.SyncRoot.EnterReadLock();
            try
            {
                return this.content.Count();
            }
            finally
            {
                this.SyncRoot.ExitReadLock();
            }
        }

        public List<T> ToList()
        {
            this.SyncRoot.EnterReadLock();
            try
            {
                return this.content.ToList();
            }
            finally
            {
                this.SyncRoot.ExitReadLock();
            }
        }
    }
}
