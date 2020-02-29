namespace wave.emit.opcodes
{
    using runtime.emit.@unsafe;

    public class F_MV : Fragment, IArg
    {
        protected readonly byte _register;
        protected readonly byte _slot;

        public F_MV(string register, string slot)
            : this(Storage.GetRegisterByLabel(register), Storage.GetSlotByLabel(slot))
        { }

        public F_MV(byte register, byte slot) : base(OpCodeValues.mv)
        {
            _register = register;
            _slot = slot;
        }

        public byte Get()
            => d8u.Null.Construct(_register, _slot);

        protected override string ToTemplateString()
            => $":mv {Storage.GetRegisterByIndex(_register)}, {Storage.GetSlotByIndex(_slot)}";
    }
}