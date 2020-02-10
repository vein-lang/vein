namespace wave.emit.opcodes
{
    public class NOP : Fragment
    {
        public NOP() : base(0x0)
        {
        }

        protected override string ToTemplateString()
            => ":nop";
    }
}