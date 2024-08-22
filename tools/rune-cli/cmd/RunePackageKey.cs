namespace vein.cmd;

using System.Globalization;

[TypeConverter(typeof(RunePackageKeyConverter))]
public readonly struct RunePackageKey(string fullName)
{
    public string Name => fullName.Split('@').First();
    public string Version => fullName.Contains("@") ? fullName.Split("@").Last() : "latest";
}

public class RunePackageKeyConverter : TypeConverter
{
    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
        => sourceType == typeof(string);

    public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
    {
        if (value is string s) return new RunePackageKey(s);
        throw new InvalidOperationException();
    }
}
