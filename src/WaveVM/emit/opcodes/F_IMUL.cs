namespace wave.emit.opcodes
{
    public class F_IMUL : FragmentWithRegisterSlot
    {

        protected override string ToTemplateString()
            => $":imul {Storage.GetRegisterByIndex(_register)}, {Storage.GetSlotByIndex(_slot)}";

        public F_IMUL(string register, string slot) : base(register, slot, OpCodeValues.imul)  { }
        public F_IMUL(byte register, byte slot) : base(register, slot , OpCodeValues.imul) { }
    }
}