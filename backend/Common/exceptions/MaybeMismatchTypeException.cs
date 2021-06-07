namespace mana.exceptions
{
    using System;
    using runtime;

    public class MaybeMismatchTypeException : Exception
    {
        public MaybeMismatchTypeException(ManaField field, ValueWasIncorrectException exp)
            : base($"field: '{field.FullName}'", exp)
        {

        }
    }
}