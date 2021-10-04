namespace vein.runtime
{
    using System;

    public class InvalidLabelException : Exception
    {
        public InvalidLabelException() : base("Incorrect label position.")
        {

        }
    }
}
