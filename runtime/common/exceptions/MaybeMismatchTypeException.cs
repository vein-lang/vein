namespace vein.exceptions
{
    using System;
    using vein.runtime;

    public class MaybeMismatchTypeException : Exception
    {
        public MaybeMismatchTypeException(VeinField field, ValueWasIncorrectException exp)
            : base($"field: '{field.FullName}'", exp)
        {

        }
    }
}
