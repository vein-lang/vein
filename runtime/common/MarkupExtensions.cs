namespace ishtar
{
    public static class MarkupExtensions
    {
        public static string EscapeArgumentSymbols(this string str)
            => str.Replace("{", "{{").Replace("}", "}}");

        public static string Escapes(this string str, char c)
            => $"{c}{str}{c}";
    }
}
