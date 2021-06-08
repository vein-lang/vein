namespace mana.runtime
{
    using System;

    public class InvalidOpCodeException : Exception
    {
        public InvalidOpCodeException(string msg) : base(msg)
        {

        }
    }
}
