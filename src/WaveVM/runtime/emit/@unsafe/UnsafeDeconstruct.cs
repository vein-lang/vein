namespace wave.runtime.emit.@unsafe
{
    using System;

    public unsafe class UnsafeDeconstruct<TValue, Self> where TValue : unmanaged where Self : UnsafeDeconstruct<TValue, Self>, new()
    {
        public static readonly Self Null = new Self();

        protected internal TValue _value { get; set; }
        protected internal Func<int> shift => shifter.Shift;

        protected internal IShifter shifter { get; private set; }
        protected internal int unmanaged_size => sizeof(TValue);
        protected internal int full_size => unmanaged_size * 2 * 4;
        protected internal int size => full_size - 4;

        public TValue Value => _value;

        protected internal void resetShifter() => shifter = ShiftFactory.Create(size);

        protected UnsafeDeconstruct(TValue value)
        {
            _value = value;
            resetShifter();
        }
    }
}