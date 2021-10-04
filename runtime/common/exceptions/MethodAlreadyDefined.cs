namespace vein.exceptions
{
    using System;

    public class MethodAlreadyDefined : Exception
    {
        public MethodAlreadyDefined(string msg) : base(msg)
        {

        }
    }
    public class FieldAlreadyDefined : Exception
    {
        public FieldAlreadyDefined(string msg) : base(msg)
        {

        }
    }
}
