namespace wave
{
    public abstract class Fragment
    {
        private readonly byte _code;

        protected Fragment(byte code) => _code = code;

        public static implicit operator byte(Fragment f) => f._code;

        protected abstract string ToTemplateString();
    }
}