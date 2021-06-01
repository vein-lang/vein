namespace ishtar
{
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;

    public static unsafe class StringStorage
    {
        internal static readonly Dictionary<string, ulong> storage_r = new();
        internal static readonly Dictionary<ulong, string> storage_l = new();

        public static StrRef* Intern(string value)
        {
            if (storage_r.ContainsKey(value))
                return (StrRef*)storage_r[value];
            var p = (ulong)storage_r.Count + 1;
            storage_r.Add(value, p);
            storage_l.Add(p, value);
            return (StrRef*)p;
        }

        public static string GetString(StrRef* p)
        {
            FFI.StaticValidate(p);
            if (!storage_l.ContainsKey((ulong)p))
            {
                VM.FastFail(WNE.ACCESS_VIOLATION, "Pointer incorrect.");
                VM.ValidateLastError();
                return null;
            }
            return storage_l[(ulong)p];
        }
    }
}