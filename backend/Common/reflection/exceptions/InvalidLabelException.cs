namespace insomnia.emit
{
    using System;

    public class InvalidLabelException : Exception 
    {
        public InvalidLabelException() : base("Incorrect label position.")
        {
            
        }
    }
}