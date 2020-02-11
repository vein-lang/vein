namespace wave.emit.opcodes
{
    public class F_IDIV : FragmentWithRegisterSlot
    {
        protected override string ToTemplateString()
            => $":idiv {Storage.GetRegisterByIndex(_register)}, {Storage.GetSlotByIndex(_slot)}";

        public F_IDIV(string register, string slot) : base(register, slot, OpCodeValues.idiv) { }
        public F_IDIV(byte register, byte slot) : base(register, slot, OpCodeValues.idiv) { }
    }
}