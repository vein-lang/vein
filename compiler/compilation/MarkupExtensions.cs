namespace insomnia.compilation
{
    public static class MarkupExtensions
    {
        public static string EscapeArgumentSymbols(this string str) 
            => str.Replace("{", "{{").Replace("}", "}}");
    }
}