namespace vein.extensions;

using System;
using Microsoft.VisualBasic;

public static class StringExtensions
{
    public static string AssertNotNull(this string value)
    {
        if (string.IsNullOrEmpty(value))
            throw new ArgumentNullException(nameof(value));
        return value;
    }
}
