namespace vein.syntax
{
    using System;
    using System.Linq;
    using ishtar;
    using Spectre.Console;

    public static class ErrorDiff
    {
        public static string DiffErrorFull(this Transform t, DocumentDeclaration doc)
        {
            try
            {
                var (diff, arrow_line) = DiffError(t, doc);
                if (diff is null && arrow_line is null)
                    return "";
                return $"\n\t[grey] {diff.EscapeMarkup().EscapeArgumentSymbols()} [/]\n\t[red] {arrow_line.EscapeMarkup().EscapeArgumentSymbols()} [/]";
            }
            catch
            {
                return ""; // TODO analytic
            }
        }

        private static (string line, string arrow_line) NewDiffError(Transform t, DocumentDeclaration doc)
        {
            if (doc is null)
                return default;
            var line = doc.SourceLines[t.pos.Line].Length < t.len ?
                t.pos.Line :
                t.pos.Line - 1;

            var original = doc.SourceLines[line];

            int takeLen()
            {
                var r = original.Skip(t.pos.Column - 1).Take(t.len).ToArray().Last();
                if (r == ' ' || r == ';' || r == ',')
                    return t.len - 1;
                return t.len;
            }

            var err_line = original.Skip(t.pos.Column-1).Take(takeLen()).ToArray();
            var space1 = original[..(t.pos.Column - 1)];
            var space2 = (t.pos.Column - 1) + t.len > original.Length ? "" : original[((t.pos.Column - 1) + t.len)..];

            return (original,
                $"{new string(' ', space1.Length)}{new string('^', err_line.Length)}{new string(' ', space2.Length)}");
        }

        public static (string line, string arrow_line) DiffError(this Transform t, DocumentDeclaration doc)
        {
            try
            {
                return NewDiffError(t, doc);
            }
            catch { }

            var line = doc.SourceLines[t.pos.Line].Length < t.len ?
                t.pos.Line - 1 : 
                /*t.pos.Line*/throw new Exception("cannot detect line");

            var original = doc.SourceLines[line];
            var err_line = original[(t.pos.Column - 1)..];
            var space1 = original[..(t.pos.Column - 1)];
            var space2 = (t.pos.Column - 1) + t.len > original.Length ? "" : original[((t.pos.Column - 1) + t.len)..];

            return (original,
                $"{new string(' ', space1.Length)}{new string('^', err_line.Length)}{new string(' ', space2.Length)}");
        }
    }
}
