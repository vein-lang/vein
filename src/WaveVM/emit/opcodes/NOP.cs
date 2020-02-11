namespace wave.emit.opcodes
{
    public class NOP : Fragment
    {
        public NOP() : base(OpCodeValues.nop)
        {
        }

        protected override string ToTemplateString()
            => ":nop";
    }
}