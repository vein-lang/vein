namespace wave.runtime.emit.@unsafe
{
    public class d32u : UnsafeDeconstruct<int, d32u>
    {
        public d32u() : base(0) { }
        public d32u(int value) : base(value) { }

        public void Deconstruct(out byte n1, out byte n2, out byte n3, out byte n4, out byte n5, out byte n6, out byte n7, out byte n8)
        {
            n1 = (byte)((_value & 0xF0000000) >> shift());
            n2 = (byte)((_value & 0x0F000000) >> shift());
            n3 = (byte)((_value & 0x00F00000) >> shift());
            n4 = (byte)((_value & 0x000F0000) >> shift());
            n5 = (byte)((_value & 0x0000F000) >> shift());
            n6 = (byte)((_value & 0x00000F00) >> shift());
            n7 = (byte)((_value & 0x000000F0) >> shift());
            n8 = (byte)((_value & 0x0000000F) >> shift());
            resetShifter();
        }
        public (byte n1, byte n2, byte n3, byte n4, byte n5, byte n6, byte n7, byte n8) Deconstruct()
        {
            var (n1, n2, n3, n4, n5, n6, n7, n8) = this;
            return (n1, n2, n3, n4, n5, n6, n7, n8);
        }
        public d32u Construct(in byte n1, in byte n2, in byte n3, in byte n4, in byte n5, in byte n6, in byte n7, in byte n8)
        {
            resetShifter();
            _value = (
                (n1 << shift()) |
                (n2 << shift()) |
                (n3 << shift()) |
                (n4 << shift()) |
                (n5 << shift()) |
                (n6 << shift()) |
                (n7 << shift()) |
                (n8 << shift()));
            return this;
        }

        public static implicit operator (byte n1, byte n2, byte n3, byte n4, byte n5, byte n6, byte n7, byte n8)(d32u u) => u.Deconstruct();
        public static implicit operator d32u((byte n1, byte n2, byte n3, byte n4, byte n5, byte n6, byte n7, byte n8) u) => new d32u(0).Construct(u.n1, u.n2, u.n3, u.n4, u.n5, u.n6, u.n7, u.n8);

        public static implicit operator int(d32u u) => u.Value;
    }
}