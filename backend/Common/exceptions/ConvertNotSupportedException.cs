namespace mana.exceptions
{
    using System;
    using runtime;

    public class ConvertNotSupportedException : Exception
    {
        public ConvertNotSupportedException(ManaTypeCode typeCode)
            : base($"Cannot get converted, '{typeCode}' is not supported.") { }

        public ConvertNotSupportedException(ManaField field)
            : base($"Cannot get converted, '{field.FullName}' with '{field.FieldType.FullName.NameWithNS}' type is not supported.") { }
    }
}