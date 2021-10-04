namespace vein.exceptions
{
    using System;
    using vein.runtime;

    public class ValueWasIncorrectException : Exception
    {
        public ValueWasIncorrectException(string value, VeinTypeCode typeCode, Exception inner)
            : base($"Value: '{value}', Type: '{typeCode}', {inner.Message}", inner)
        { }
    }
}
