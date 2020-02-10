namespace wave
{
    using System.Collections.Generic;
    using System.Linq;

    public static class Storage
    {
        private static readonly Dictionary<byte, string> regs = new Dictionary<byte, string>
        {
            {0, "sra"},
            {1, "srb"},
            {2, "src"},
            {3, "srd"},
            {4, "sre"},
            {5, "srf"},
        };

        private static readonly Dictionary<byte, string> slots = new Dictionary<byte, string>
        {
            {0, "isa"},
            {1, "isb"},
            {2, "isc"},
            {3, "isd"},
            {4, "ise"},
            {5, "isf"},
        };

        public static byte GetRegisterByLabel(string label)
            => regs.FirstOrDefault(x => x.Value == label).Key;

        public static string GetRegisterByIndex(byte index)
            => regs[index];

        public static byte GetSlotByLabel(string label)
            => slots.FirstOrDefault(x => x.Value == label).Key;

        public static string GetSlotByIndex(byte index)
            => slots[index];
    }
}