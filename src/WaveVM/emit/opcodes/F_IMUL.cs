namespace wave
{
    public class F_IMUL : F_MV
    {
        public F_IMUL(string register, string slot) : base(register, slot)
        {
        }

        public F_IMUL(byte register, byte slot) : base(register, slot)
        {
        }

        protected override string ToTemplateString()
            => $":imul {Storage.GetRegisterByIndex(_register)}, {Storage.GetSlotByIndex(_slot)}";
    }
}