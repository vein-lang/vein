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
                var (diff, arrow_line) = DiffError(t, doc.SourceLines);
                if (diff is null && arrow_line is null)
                    return "";
                return $"\n\t[grey] {diff.EscapeMarkup().EscapeArgumentSymbols()} [/]\n\t[red] {arrow_line.EscapeMarkup().EscapeArgumentSymbols()} [/]";
            }
            catch
            {
                return ""; // TODO analytic
            }
        }

        public static string DiffErrorFull(this Transform t, FileInfo doc)
        {
            try
            {
                var (diff, arrow_line) = DiffError(t, doc.ReadAllLines());
                if (diff is null && arrow_line is null)
                    return "";
                return $"\n\t[grey] {diff.EscapeMarkup().EscapeArgumentSymbols()} [/]\n\t[red] {arrow_line.EscapeMarkup().EscapeArgumentSymbols()} [/]";
            }
            catch
            {
                return ""; // TODO analytic
            }
        }

        private static (string line, string arrow_line) NewDiffError(Transform t, string[] sourceLines)
        {
            if (sourceLines is null)
                return default;
            if (sourceLines.Length == 0)
                return default;
            var line = sourceLines[t.pos.Line - 1].Length < t.len ?
                t.pos.Line :
                t.pos.Line - 1;

            var original = sourceLines[line - 1];

            int takeLen()
            {
                var r = original.Skip(t.pos.Column - 1).Take(t.len).ToArray().Last();
                if (r is ' ' or ';' or ',')
                    return t.len - 1;
                return t.len;
            }

            var err_line = original.Skip(t.pos.Column-1).Take(takeLen()).ToArray();
            var space1 = original[..(t.pos.Column - 1)];
            var space2 = (t.pos.Column - 1) + t.len > original.Length ? "" : original[((t.pos.Column - 1) + t.len)..];

            return (original,
                $"{new string(' ', space1.Length)}{new string('^', err_line.Length)}{new string(' ', space2.Length)}");
        }

        public static (string line, string arrow_line) DiffError(this Transform t, string[] sourceLines)
        {
            try
            {
                return NewDiffError(t, sourceLines);
            }
            catch { }

            var line = sourceLines[t.pos.Line].Length < t.len ?
                t.pos.Line - 1 : 
                /*t.pos.Line*/throw new Exception("cannot detect line");

            var original = sourceLines[line];
            var err_line = original[(t.pos.Column - 1)..];
            var space1 = original[..(t.pos.Column - 1)];
            var space2 = (t.pos.Column - 1) + t.len > original.Length ? "" : original[((t.pos.Column - 1) + t.len)..];

            return (original,
                $"{new string(' ', space1.Length)}{new string('^', err_line.Length)}{new string(' ', space2.Length)}");
        }


    }
}
