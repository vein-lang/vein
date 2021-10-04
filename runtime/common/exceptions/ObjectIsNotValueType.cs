namespace mana.exceptions
{
    using System;

    public class ObjectIsNotValueType : Exception
    {
        public ObjectIsNotValueType(string msg) : base(msg)
        {

        }
    }
}
