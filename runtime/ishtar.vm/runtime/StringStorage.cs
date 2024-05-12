namespace ishtar
{
    using System.Collections.Generic;

    public static unsafe class StringStorage
    {
        internal static readonly Dictionary<string, ulong> storage_r = new();
        internal static readonly Dictionary<ulong, string> storage_l = new();

        public static InternedString* Intern(string value)
        {
            if (storage_r.TryGetValue(value, out ulong value1))
                return (InternedString*)value1;
            var p = (ulong)storage_r.Count + 1;
            storage_r.Add(value, p);
            storage_l.Add(p, value);
            return (InternedString*)p;
        }

        public static string GetString(InternedString* p, CallFrame frame)
        {
            ForeignFunctionInterface.StaticValidate(p, frame);
            if (storage_l.ContainsKey((ulong)p))
                return storage_l[(ulong)p];
            frame.vm.FastFail(WNE.ACCESS_VIOLATION, "Pointer incorrect.", frame);
            return null;
        }

        public static string GetStringUnsafe(InternedString* p)
            => !storage_l.ContainsKey((ulong)p) ? null : storage_l[(ulong)p];
    }


    public struct InternedString(ulong id);
}
