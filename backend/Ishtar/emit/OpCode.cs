namespace mana.ishtar.emit
{
    using System;
    using global::ishtar;
    using global::runtime.runtime.emit;

    public readonly struct OpCode : IEquatable<OpCode>
    {
        private readonly OpCodeValue value;
        private readonly int flags;

        internal OpCode(int value, int flags) : this((OpCodeValue)value, flags)
        {
        }
        internal OpCode(OpCodeValue value, int flags)
        {
            this.value = value;
            this.flags = flags;
        }

        #region Equality members

        public bool Equals(OpCode other) => value == other.value && flags == other.flags;

        public override bool Equals(object obj) => obj is OpCode other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                return ((int)value * 0x18D) ^ flags;
            }
        }

        public static bool operator ==(OpCode left, OpCode right) => left.Equals(right);

        public static bool operator !=(OpCode left, OpCode right) => !left.Equals(right);

        #endregion

        private static string[] _cache_names;

        public string Name
        {
            get
            {
                _cache_names ??= new string[Enum.GetValues(typeof(OpCodeValue)).Length];
                return _cache_names[(ushort)value] ?? (_cache_names[(ushort)value] = Enum
                        .GetName(typeof(OpCodeValue), value)!
                    .ToLowerInvariant()
                    .Replace("_", "."));
            }
        }

        public ushort Value => (ushort)this.value;


        public ControlChain ControlChain => (ControlChain)(flags >> 0xC & 0x1F);

        public FlowControl FlowControl => (FlowControl)(flags >> 0x11 & 0x1F);

        public int Size => flags >> 0x16 & 0x1F;

        internal static int CreateFlag(byte size, FlowControl flow, ControlChain chain)
            => ((int)chain << 0xC) | 0x1F | ((int)flow << 0x11) | 0x1F | (size << 22) | 0x1F;
    }
}
