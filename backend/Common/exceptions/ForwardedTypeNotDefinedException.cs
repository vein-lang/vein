namespace mana.exceptions
{
    using System;

    public class ForwardedTypeNotDefinedException : Exception
    {
        public ForwardedTypeNotDefinedException(string type_name)
            : base($"Forwarded type '{type_name}' is not defined.") { }
    }
}