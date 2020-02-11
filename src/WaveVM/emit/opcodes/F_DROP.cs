namespace wave.emit.opcodes
{
    public class F_DROP : Fragment
    {
        public F_DROP() : base(OpCodeValues.drop)
        {
        }

        protected override string ToTemplateString()
            => ":drop";
    }
}