namespace mana.runtime
{
    using System;

    public class ConstCannotUseNonPrimitiveTypeException : Exception
    {
        public ConstCannotUseNonPrimitiveTypeException(FieldName name, Type type) :
            base($"'{name}' trying use {type}, is not allowed.")
        {

        }
    }
}