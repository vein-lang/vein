namespace wave
{
    public class F_DROP : Fragment
    {
        public F_DROP() : base(0x1)
        {
        }

        protected override string ToTemplateString()
            => ":drop";
    }
}