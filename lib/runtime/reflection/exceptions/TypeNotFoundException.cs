namespace wave.emit
{
    using System;

    public class TypeNotFoundException : Exception
    {
        public TypeNotFoundException(string msg) : base(msg) { }
    }
}