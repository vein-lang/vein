namespace wave.emit.opcodes
{
    using System;
    using runtime.kernel.@unsafe;

    public class F_LABEL : Fragment, IInterningProvider, IArgs
    {
        private readonly string _labelId;

        public F_LABEL(string labelID) : base(OpCodeValues.label)
            => _labelId = labelID;

        protected override string ToTemplateString()
            => $"+{_labelId}:";

        public string[] GetForInterning()
        {
            return new[] { _labelId };
        }

        public byte[] Get() 
            => BitConverter.GetBytes(NativeString.Wrap(_labelId).GetHashCode());
    }
}