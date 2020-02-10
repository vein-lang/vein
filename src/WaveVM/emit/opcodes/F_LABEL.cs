namespace wave.emit.opcodes
{
    public class F_LABEL : Fragment, IInterningProvider
    {
        private readonly string _labelId;

        public F_LABEL(string labelID) : base(0x2)
            => _labelId = labelID;

        protected override string ToTemplateString()
            => $"+{_labelId}:";

        public string[] GetForInterning()
        {
            return new[] { _labelId };
        }
    }
}