namespace ishtar
{
    using System.Collections.Generic;
    using System.Runtime.InteropServices;

    public static unsafe class StringStorage
    {
        internal static readonly Dictionary<string, nint> storage_r = new();
        internal static readonly Dictionary<nint, string> storage_l = new();

        public static InternedString* Intern(string value)
        {
            if (storage_r.TryGetValue(value, out var value1))
                return (InternedString*)value1;
            var p = Marshal.StringToHGlobalUni(value);
            storage_r.Add(value, p);
            storage_l.Add(p, value);
            return (InternedString*)p;
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


    public readonly unsafe struct InternedString(ulong id)
    {
        private readonly ulong ID = id;
        public bool Equals(InternedString* st) => st->ID == ID;
    }
}
