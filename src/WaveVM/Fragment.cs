namespace wave
{
    using emit.opcodes;
    using runtime.emit.@unsafe;

    public abstract class Fragment
    {
        private readonly OpCodeValues _code;

        protected Fragment(OpCodeValues code) => _code = code;

        public static implicit operator byte(Fragment f) => (byte)f._code;

        protected abstract string ToTemplateString();
    }

    public abstract class FragmentWithRegisterSlot : Fragment, IArg<byte>
    {
        protected readonly byte _register;
        protected readonly byte _slot;

        protected FragmentWithRegisterSlot(string register, string slot, OpCodeValues code) : base(code)
        {
            _register = Storage.GetRegisterByLabel(register);
            _slot = Storage.GetSlotByLabel(slot);
        }

        protected FragmentWithRegisterSlot(byte register, byte slot, OpCodeValues code) : base(code)
        {
            _register = register;
            _slot = slot;
        }

        public byte Get()
            => d8u.Null.Construct(_register, _slot);
    }
}