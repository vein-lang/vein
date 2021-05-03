namespace ishtar
{
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;

    public static unsafe class StringStorage
    {
        private static readonly Dictionary<string, ulong> storage_r = new();
        private static readonly Dictionary<ulong, string> storage_l = new();

        public static StrRef* Intern(string value)
        {
            if (storage_r.ContainsKey(value))
                return (StrRef*)storage_r[value];

            var p = (StrRef*) Marshal.AllocHGlobal(sizeof(StrRef));
            
            Unsafe.InitBlock(p, 0, (uint)sizeof(StrRef));
            
            if (p == null)
            {
                VM.FastFail(WNE.OUT_OF_MEMORY, "Cannot allocate string reference.");
                VM.ValidateLastError();
                return null;
            }
            p->index = (ulong)storage_r.Count + 1;
            storage_r.Add(value, p->index);
            storage_l.Add(p->index, value);
            return p;
        }

        public static string GetString(StrRef* p)
        {
            FFI.StaticValidate(p);
            if (!storage_l.ContainsKey(p->index))
            {
                VM.FastFail(WNE.ACCESS_VIOLATION, "Pointer incorrect.");
                VM.ValidateLastError();
                return null;
            }
            return storage_l[p->index];
        }
    }
}