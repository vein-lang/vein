namespace wave.emit
{
    using System;

    public class FieldIsNotDeclaredException : Exception
    {
        public FieldIsNotDeclaredException(FieldName field) : base($"Field '{field.name}' is not declared.")
        {
            
        }
    }
}