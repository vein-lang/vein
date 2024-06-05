namespace ishtar
{
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using collections;
    using runtime.gc;
    using runtime;
    using vein.runtime;

    public static unsafe class StringStorage
    {
        internal static readonly Dictionary<string, nint> storage_r = new();
        internal static readonly Dictionary<nint, string> storage_l = new();

        internal static readonly Dictionary<ulong, string> storage_d = new();
        internal static readonly Dictionary<ulong, nint> storage_d_hglobal = new();

        internal static ulong _indexer;

        public static InternedString* Intern(string value)
        {
            if (storage_r.TryGetValue(value, out var value1))
                return (InternedString*)value1;

            var str = IshtarGC.AllocateImmortal<InternedString>();

            *str = new InternedString(Interlocked.Increment(ref _indexer));

            var p = (InternedString*)Marshal.StringToHGlobalUni(value);

            storage_d.Add(str->ID, value);
            storage_r.Add(value, (nint)str);
            storage_l.Add((nint)str, value);
            storage_d_hglobal.Add(str->ID, (nint)p);
            return str;
        }

        public static void Dispose()
        {
            //foreach (var (id, ptr) in storage_d_hglobal)
            //    Marshal.FreeHGlobal(ptr);
            //storage_d_hglobal.Clear();
            //foreach (var (id, ptr) in storage_r)
            //    IshtarGC.FreeImmortal((InternedString*)ptr);
            //storage_r.Clear();
            //storage_d.Clear();
            //storage_l.Clear();
        }

        public static string GetString(InternedString* p, CallFrame frame)
        {
            ForeignFunctionInterface.StaticValidate(p, frame);
            if (storage_l.ContainsKey((nint)p))
                return storage_l[(nint)p];
            frame.vm.FastFail(WNE.ACCESS_VIOLATION, "Pointer incorrect.", frame);
            return null;
        }

        public static string GetStringUnsafe(InternedString* p)
        {
            if (!storage_l.ContainsKey((nint)p))
                throw new KeyNotFoundException();
            else
                return storage_l[(nint)p];
        }
    }


    [DebuggerDisplay("{debugStr}")]
    public readonly unsafe struct InternedString(ulong id) : IEq<InternedString>
    {
        public readonly ulong ID = id;
        public bool Equals(InternedString* st) => st->ID == ID;

        private string debugStr => StringStorage.storage_d[id];
        public static bool Eq(InternedString* p1, InternedString* p2) => p1->ID.Equals(p2->ID);
    }
}
