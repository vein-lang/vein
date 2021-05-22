namespace mana.exceptions
{
    using System;
    using runtime;

    public class ValueWasIncorrectException : Exception
    {
        public ValueWasIncorrectException(string value, ManaTypeCode typeCode, Exception inner) 
            : base($"Value: '{value}', Type: '{typeCode}', {inner.Message}", inner)
        { }
    }
}