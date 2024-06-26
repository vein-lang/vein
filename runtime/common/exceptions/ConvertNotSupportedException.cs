namespace vein.exceptions
{
    using System;
    using runtime;

    public class ConvertNotSupportedException : Exception
    {
        public ConvertNotSupportedException(VeinTypeCode typeCode)
            : base($"Cannot get converted, '{typeCode}' is not supported.") { }

        public ConvertNotSupportedException(VeinField field)
            : base(
                $"Cannot get converted, '{field.FullName}' with '{field.FieldType.ToTemplateString()}' type is not supported.") { }
    }
}
