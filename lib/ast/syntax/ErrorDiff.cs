namespace vein.syntax;

using System;
using System.Diagnostics;
using System.Linq;
using ishtar;
using Spectre.Console;

public static class ErrorDiff
{
    public static string DiffErrorFull(this Transform t, DocumentDeclaration doc)
    {
        try
        {
            return FormatError(t, doc.SourceLines);
        }
        catch { }

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

    private static string FormatError(Transform position, string[] sources)
    {
        if (position.pos.Line <= 0 || position.pos.Line > sources.Length)
            throw new ArgumentOutOfRangeException(nameof(position.pos.Line), "Line number is out of range.");

        var line = sources[position.pos.Line - 1];

        if (position.pos.Column <= 0 || position.pos.Column > line.Length)
            throw new ArgumentOutOfRangeException(nameof(position.pos.Column), "Column number is out of range.");

        if (position.pos.Column - 1 + position.len > line.Length)
            throw new ArgumentOutOfRangeException(nameof(position.len), "Length is out of range.");
            
        var formattedError =
            $"\n\t[grey]{line.EscapeMarkup()}[/]" +
            $"\n\t{new string(' ', position.pos.Column - 1)}[red]{new string('^', position.len)}[/]";

        return formattedError;
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
        catch(Exception e)
        {
            Trace.WriteLine(e.ToString());
            return ""; // TODO analytic
        }
    }

    private static (string line, string arrow_line) NewDiffError(Transform transform, string[] sourceLines)
    {
        if (transform is null)
            throw new ArgumentNullException(nameof(transform));
        if (sourceLines is null)
            throw new ArgumentNullException(nameof(sourceLines));
        if (sourceLines.Length == 0)
            throw new ArgumentOutOfRangeException(nameof(sourceLines));
        var line = sourceLines[transform.pos.Line - 1].Length < transform.len ?
            transform.pos.Line :
            transform.pos.Line - 1;

        var original = sourceLines[line - 1];

        int takeLen()
        {
            var r = original.Skip(transform.pos.Column - 1).Take(transform.len).ToArray().Last();
            if (r is ' ' or ';' or ',')
                return transform.len - 1;
            return transform.len;
        }

        var err_line = original.Skip(transform.pos.Column-1).Take(takeLen()).ToArray();
        var space1 = original[..(transform.pos.Column - 1)];
        var space2 = (transform.pos.Column - 1) + transform.len > original.Length ? "" : original[((transform.pos.Column - 1) + transform.len)..];

        return (original,
            $"{new string(' ', space1.Length)}{new string('^', err_line.Length)}{new string(' ', space2.Length)}");
    }

    public static (string line, string arrow_line) DiffError(this Transform t, string[] sourceLines)
    {
        try
        {
            return NewDiffError(t, sourceLines);
        }
        catch (Exception e)
        {
            Trace.WriteLine(e.ToString());
        }

        (string line, string arrow_line) cast(int line)
        {
            var original = sourceLines[line];
            var err_line = original[(t.pos.Column - 1)..];
            var space1 = original[..(t.pos.Column - 1)];
            var space2 = (t.pos.Column - 1) + t.len > original.Length ? "" : original[((t.pos.Column - 1) + t.len)..];

            return (original,
                $"{new string(' ', space1.Length)}{new string('^', err_line.Length)}{new string(' ', space2.Length)}");
        }


        try
        {
            return cast(t.pos.Line - 1);
        }
        catch (Exception e)
        {
            Trace.WriteLine(e.ToString());
        }
        try
        {
            return cast(t.pos.Line);
        }
        catch (Exception e)
        {
            Trace.WriteLine(e.ToString());
        }

        throw new Exception($"Cant detect line");
    }
}
