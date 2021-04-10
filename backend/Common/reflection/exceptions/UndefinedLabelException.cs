namespace wave.runtime
{
    using System;

    public class UndefinedLabelException : Exception
    {
        public UndefinedLabelException() : base("Label is not defined in this generator.")
        {
            
        }
    }
}