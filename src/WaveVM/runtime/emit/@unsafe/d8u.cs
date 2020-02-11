namespace wave.runtime.emit.@unsafe
{
    public class d8u : UnsafeDeconstruct<byte, d8u>
    {
        public d8u() : base(0)
        {
        }

        public d8u(byte value) : base(value)
        {
        }

        public void Deconstruct(out byte n1, out byte n2)
        {
            n1 = (byte)((_value & 0xF0) >> shift());
            n2 = (byte)((_value & 0x0F) >> shift());
            resetShifter();
        }

        public (byte n1, byte n2) Deconstruct()
        {
            var (n1, n2) = this;
            return (n1, n2);
        }

        public d8u Construct(in byte n1, in byte n2)
        {
            resetShifter();
            this._value = (byte)((n1 << shift()) | (n2 << shift()));
            return this;
        }

        public static implicit operator (byte n1, byte n2)(d8u u) => u.Deconstruct();

        public static implicit operator d8u((byte n1, byte n2, byte n3, byte n4) u) => new d8u(0).Construct(u.n1, u.n2);

        public static implicit operator byte(d8u u) => u.Value;
    }
}